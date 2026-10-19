using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.Orders;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using NotFoundException = OmniHogar.Domain.Exceptions.NotFoundException;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Tests.Features.Orders;

public class ShippingLabelTests
{
    private static async Task<(InMemoryApplicationDbContext Context, Order Order, FakeCurrentUserService User)> Seed(
        string status, bool withAddress = true)
    {
        var context = InMemoryApplicationDbContext.Create();

        var customer = new User
        {
            UserType = UserType.customer,
            FirstName = "Ana",
            LastName = "Gómez",
            Email = "ana@example.com",
            Phone = "3001234567",
            PasswordHash = "hash",
        };
        var coordinator = new User
        {
            UserType = UserType.employee,
            FirstName = "Carlos",
            LastName = "Despacho",
            Email = "carlos@example.com",
            PasswordHash = "hash",
        };
        var product = new Product { Sku = "SKU-1", Name = "Silla", Price = 100m, Status = "active" };
        context.Users.AddRange(customer, coordinator);
        context.Products.Add(product);

        CustomerAddress? address = null;
        if (withAddress)
        {
            address = new CustomerAddress
            {
                UserId = customer.Id,
                Address = "Calle 10 #20-30",
                City = "Medellín",
                Neighborhood = "El Poblado",
                Reference = "Portería",
            };
            context.CustomerAddresses.Add(address);
        }

        var order = new Order
        {
            OrderNumber = "ORD-500",
            UserId = customer.Id,
            User = customer,
            Channel = "web",
            Status = status,
            Subtotal = 200m,
            Total = 200m,
            ShippingAddressId = address?.Id,
            ShippingAddress = address,
        };
        order.Items.Add(new OrderItem { Order = order, ProductId = product.Id, Product = product, Quantity = 2, UnitPrice = 100m, Subtotal = 200m });
        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, order, new FakeCurrentUserService { UserId = coordinator.Id.ToString() });
    }

    [Fact]
    public async Task PackedOrder_GeneratesLabelWithOrderAddressAndShippingData()
    {
        var (context, order, user) = await Seed("packed");
        var handler = new GenerateShippingLabelCommandHandler(context, user);

        var result = await handler.Handle(new GenerateShippingLabelCommand(order.Id), CancellationToken.None);

        Assert.Equal(order.Id, result.OrderId);
        Assert.Equal("ORD-500", result.OrderNumber);
        Assert.StartsWith("OH-", result.TrackingNumber);
        Assert.False(string.IsNullOrWhiteSpace(result.Carrier));
        Assert.Equal("Calle 10 #20-30", result.Address);
        Assert.Equal("Medellín", result.City);
        Assert.Equal("Ana Gómez", result.CustomerName);
        Assert.Equal("3001234567", result.CustomerPhone);
        var line = Assert.Single(result.Items);
        Assert.Equal("Silla", line.ProductName);
        Assert.Equal(2, line.Quantity);

        var dispatch = await context.Dispatches.SingleAsync();
        Assert.Equal("packed", dispatch.Status);
        Assert.Equal(Guid.Parse(user.UserId!), dispatch.HandlerId);
        var label = await context.ShippingLabels.SingleAsync();
        Assert.Equal(dispatch.Id, label.DispatchId);
    }

    [Fact]
    public async Task OrderNotPackedYet_IsRejectedAndNoLabelIsCreated()
    {
        var (context, order, user) = await Seed("preparing");
        var handler = new GenerateShippingLabelCommandHandler(context, user);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new GenerateShippingLabelCommand(order.Id), CancellationToken.None));

        Assert.Contains("aún no está preparado", ex.Errors["orderId"][0]);
        Assert.Empty(context.ShippingLabels);
        Assert.Empty(context.Dispatches);
    }

    [Fact]
    public async Task OrderWithoutShippingAddress_IsRejected()
    {
        var (context, order, user) = await Seed("packed", withAddress: false);
        var handler = new GenerateShippingLabelCommandHandler(context, user);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new GenerateShippingLabelCommand(order.Id), CancellationToken.None));

        Assert.Contains("dirección de entrega", ex.Errors["orderId"][0]);
        Assert.Empty(context.ShippingLabels);
    }

    [Fact]
    public async Task SecondGeneration_IsRejected()
    {
        var (context, order, user) = await Seed("packed");
        var handler = new GenerateShippingLabelCommandHandler(context, user);
        await handler.Handle(new GenerateShippingLabelCommand(order.Id), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new GenerateShippingLabelCommand(order.Id), CancellationToken.None));

        Assert.Contains("ya tiene una guía", ex.Errors["orderId"][0]);
        Assert.Single(context.ShippingLabels);
    }

    [Fact]
    public async Task NonexistentOrder_ThrowsNotFound()
    {
        var (context, _, user) = await Seed("packed");
        var handler = new GenerateShippingLabelCommandHandler(context, user);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GenerateShippingLabelCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task GetLabel_ReturnsTheGeneratedGuide()
    {
        var (context, order, user) = await Seed("packed");
        var generated = await new GenerateShippingLabelCommandHandler(context, user)
            .Handle(new GenerateShippingLabelCommand(order.Id), CancellationToken.None);

        var result = await new GetShippingLabelQueryHandler(context)
            .Handle(new GetShippingLabelQuery(order.Id), CancellationToken.None);

        Assert.Equal(generated.TrackingNumber, result.TrackingNumber);
        Assert.Equal("Calle 10 #20-30", result.Address);
        Assert.Equal("ORD-500", result.OrderNumber);
    }

    [Fact]
    public async Task GetLabel_WithoutGeneratedGuide_ThrowsNotFound()
    {
        var (context, order, _) = await Seed("packed");

        await Assert.ThrowsAsync<NotFoundException>(
            () => new GetShippingLabelQueryHandler(context).Handle(new GetShippingLabelQuery(order.Id), CancellationToken.None));
    }
}
