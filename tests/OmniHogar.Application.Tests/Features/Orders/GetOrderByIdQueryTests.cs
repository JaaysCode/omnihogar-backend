using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniHogar.Application.Common.Mappings;
using OmniHogar.Application.Features.Orders;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Tests.Features.Orders;

public class GetOrderByIdQueryTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task ExistingOrder_ReturnsProductsQuantitiesValueClientChannelAndStatus()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var customer = new User
        {
            UserType = UserType.customer,
            FirstName = "Camila",
            LastName = "Ruiz",
            Email = "camila.ruiz@example.com",
            PasswordHash = "hash",
        };
        var product = new Product { Sku = "SKU-200", Name = "Aspiradora Robot", Price = 899_900m };
        context.Users.Add(customer);
        context.Products.Add(product);

        var order = new Order
        {
            OrderNumber = "ORD-500",
            UserId = customer.Id,
            User = customer,
            Channel = "web",
            Status = "payment_approved",
            Subtotal = 1_799_800m,
            Total = 2_141_762m,
        };
        order.Items.Add(new OrderItem
        {
            Order = order,
            ProductId = product.Id,
            Product = product,
            Quantity = 2,
            UnitPrice = 899_900m,
            Subtotal = 1_799_800m,
        });
        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetOrderByIdQueryHandler(context, CreateMapper());
        var result = await handler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);

        Assert.Equal("ORD-500", result.OrderNumber);
        Assert.Equal("web", result.Channel);
        Assert.Equal("payment_approved", result.Status);
        Assert.Equal("Camila Ruiz", result.CustomerName);
        Assert.Equal("camila.ruiz@example.com", result.CustomerEmail);
        Assert.Equal(2_141_762m, result.Total);

        var item = Assert.Single(result.Items);
        Assert.Equal("Aspiradora Robot", item.ProductName);
        Assert.Equal("SKU-200", item.Sku);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(899_900m, item.UnitPrice);
        Assert.Equal(1_799_800m, item.Subtotal);
    }

    [Fact]
    public async Task ExistingOrder_WithMultipleItems_ReturnsEachItemMappedFromItsOwnProduct()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var customer = new User
        {
            UserType = UserType.customer,
            FirstName = "Camila",
            LastName = "Ruiz",
            Email = "camila.ruiz@example.com",
            PasswordHash = "hash",
        };
        var vacuum = new Product { Sku = "SKU-200", Name = "Aspiradora Robot", Price = 899_900m };
        var blender = new Product { Sku = "SKU-100", Name = "Licuadora", Price = 120_000m };
        context.Users.Add(customer);
        context.Products.AddRange(vacuum, blender);

        var order = new Order
        {
            OrderNumber = "ORD-501",
            UserId = customer.Id,
            User = customer,
            Channel = "web",
            Status = "payment_approved",
            Subtotal = 1_919_800m,
            Total = 2_284_562m,
        };
        order.Items.Add(new OrderItem
        {
            Order = order,
            ProductId = vacuum.Id,
            Product = vacuum,
            Quantity = 2,
            UnitPrice = 899_900m,
            Subtotal = 1_799_800m,
        });
        order.Items.Add(new OrderItem
        {
            Order = order,
            ProductId = blender.Id,
            Product = blender,
            Quantity = 1,
            UnitPrice = 120_000m,
            Subtotal = 120_000m,
        });
        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetOrderByIdQueryHandler(context, CreateMapper());
        var result = await handler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);

        Assert.Equal(2, result.Items.Count);

        var vacuumItem = Assert.Single(result.Items, i => i.Sku == "SKU-200");
        Assert.Equal("Aspiradora Robot", vacuumItem.ProductName);
        Assert.Equal(2, vacuumItem.Quantity);
        Assert.Equal(899_900m, vacuumItem.UnitPrice);
        Assert.Equal(1_799_800m, vacuumItem.Subtotal);

        var blenderItem = Assert.Single(result.Items, i => i.Sku == "SKU-100");
        Assert.Equal("Licuadora", blenderItem.ProductName);
        Assert.Equal(1, blenderItem.Quantity);
        Assert.Equal(120_000m, blenderItem.UnitPrice);
        Assert.Equal(120_000m, blenderItem.Subtotal);
    }

    [Fact]
    public async Task NonexistentOrder_ThrowsNotFoundException()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var handler = new GetOrderByIdQueryHandler(context, CreateMapper());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetOrderByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
