using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.Orders;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Tests.Features.Orders;

public class RegisterStoreSaleCommandHandlerTests
{
    private static User Advisor(Guid id) =>
        new() { Id = id, UserType = UserType.employee, Email = $"{id:N}@x.test", FirstName = "A", LastName = "X", PasswordHash = "h" };

    private static async Task<(InMemoryApplicationDbContext Context, FakeCurrentUserService User, Guid ProductId, Facility Facility)> Seed(int available, decimal price = 100m)
    {
        var context = InMemoryApplicationDbContext.Create();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var facility = new Facility { Name = "Tienda Centro", Type = FacilityType.POS, Address = "Calle 1", City = "Bogotá" };

        context.Users.Add(Advisor(userId));
        context.Products.Add(new Product { Id = productId, Sku = "SKU-1", Name = "Silla", Price = price, Status = "active" });
        context.Facilities.Add(facility);
        context.Inventory.Add(new Inventory { ProductId = productId, FacilityId = facility.Id, AvailableQuantity = available });
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, new FakeCurrentUserService { UserId = userId.ToString() }, productId, facility);
    }

    [Fact]
    public async Task SuccessfulSale_CreatesOrderWithUniqueNumberAsAdvisorAndCustomer()
    {
        var (context, user, productId, facility) = await Seed(available: 10);
        var handler = new RegisterStoreSaleCommandHandler(context, user);

        var result = await handler.Handle(
            new RegisterStoreSaleCommand([new StoreSaleItem(productId, 2)]), CancellationToken.None);

        var order = await context.Orders.Include(o => o.Items).SingleAsync();
        Assert.Equal("store", order.Channel);
        Assert.Equal("payment_approved", order.Status);
        Assert.Equal(Guid.Parse(user.UserId!), order.UserId);
        Assert.Equal(Guid.Parse(user.UserId!), order.AdvisorId);
        Assert.Equal(facility.Id, order.FacilityId);
        Assert.False(string.IsNullOrWhiteSpace(order.OrderNumber));
        Assert.Equal(order.Id, result.OrderId);
        Assert.Equal(order.OrderNumber, result.OrderNumber);
    }

    [Fact]
    public async Task TwoCalls_GenerateDifferentOrderNumbers()
    {
        var (context, user, productId, _) = await Seed(available: 10);
        var handler = new RegisterStoreSaleCommandHandler(context, user);

        var first = await handler.Handle(new RegisterStoreSaleCommand([new StoreSaleItem(productId, 1)]), CancellationToken.None);
        var second = await handler.Handle(new RegisterStoreSaleCommand([new StoreSaleItem(productId, 1)]), CancellationToken.None);

        Assert.NotEqual(first.OrderNumber, second.OrderNumber);
    }

    [Fact]
    public async Task ComputesSubtotalAndAppliesNineteenPercentTax()
    {
        var (context, user, productId, _) = await Seed(available: 10, price: 250m);
        var handler = new RegisterStoreSaleCommandHandler(context, user);

        var result = await handler.Handle(
            new RegisterStoreSaleCommand([new StoreSaleItem(productId, 3)]), CancellationToken.None);

        Assert.Equal(750m, result.Subtotal);
        Assert.Equal(142.5m, result.Tax);
        Assert.Equal(892.5m, result.Total);
    }

    [Fact]
    public async Task DecrementsInventoryAndRecordsAnOutboundMovementLinkedToTheOrder()
    {
        var (context, user, productId, facility) = await Seed(available: 10);
        var handler = new RegisterStoreSaleCommandHandler(context, user);

        var result = await handler.Handle(
            new RegisterStoreSaleCommand([new StoreSaleItem(productId, 4)]), CancellationToken.None);

        var inventory = await context.Inventory.SingleAsync(i => i.ProductId == productId);
        Assert.Equal(6, inventory.AvailableQuantity);

        var movement = await context.InventoryMovements.SingleAsync();
        Assert.Equal("outbound", movement.Type);
        Assert.Equal(4, movement.Quantity);
        Assert.Equal(facility.Id, movement.FacilityId);
        Assert.Equal(result.OrderId, movement.OrderId);
    }

    [Fact]
    public async Task DuplicateProductLines_AreMergedIntoOneDeduction()
    {
        var (context, user, productId, _) = await Seed(available: 10);
        var handler = new RegisterStoreSaleCommandHandler(context, user);

        await handler.Handle(
            new RegisterStoreSaleCommand([new StoreSaleItem(productId, 2), new StoreSaleItem(productId, 3)]),
            CancellationToken.None);

        var inventory = await context.Inventory.SingleAsync(i => i.ProductId == productId);
        Assert.Equal(5, inventory.AvailableQuantity);
        var line = Assert.Single(await context.OrderItems.ToListAsync());
        Assert.Equal(5, line.Quantity);
    }

    [Fact]
    public async Task QuantityBeyondAvailable_ThrowsValidationExceptionAndWritesNothing()
    {
        var (context, user, productId, _) = await Seed(available: 2);
        var handler = new RegisterStoreSaleCommandHandler(context, user);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new RegisterStoreSaleCommand([new StoreSaleItem(productId, 5)]), CancellationToken.None));

        Assert.Contains("Disponible: 2", ex.Errors["Items"][0]);
        Assert.Empty(context.Orders);
        Assert.Empty(context.InventoryMovements);
        var inventory = await context.Inventory.SingleAsync(i => i.ProductId == productId);
        Assert.Equal(2, inventory.AvailableQuantity);
    }
}
