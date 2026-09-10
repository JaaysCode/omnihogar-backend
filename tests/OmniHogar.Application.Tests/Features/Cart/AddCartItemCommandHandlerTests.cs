using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.Cart;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Tests.Features.Cart;

public class AddCartItemCommandHandlerTests
{
    private static User Customer(Guid id) =>
        new() { Id = id, UserType = UserType.customer, Email = $"{id:N}@x.test", FirstName = "C", LastName = "X", PasswordHash = "h" };

    private static async Task<(InMemoryApplicationDbContext Context, FakeCurrentUserService User, Guid ProductId)> Seed(int available, decimal price = 100m)
    {
        var context = InMemoryApplicationDbContext.Create();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var facility = new Facility { Name = "Bodega", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" };

        context.Users.Add(Customer(userId));
        context.Products.Add(new Product { Id = productId, Sku = "SKU-1", Name = "Silla", Price = price, Status = "active" });
        context.Facilities.Add(facility);
        if (available > 0)
        {
            context.Inventory.Add(new Inventory { ProductId = productId, FacilityId = facility.Id, AvailableQuantity = available });
        }

        await context.SaveChangesAsync(CancellationToken.None);
        return (context, new FakeCurrentUserService { UserId = userId.ToString() }, productId);
    }

    [Fact]
    public async Task FirstAdd_CreatesActiveCartWithTheLine()
    {
        var (context, user, productId) = await Seed(available: 10, price: 250m);
        var handler = new AddCartItemCommandHandler(context, user);

        await handler.Handle(new AddCartItemCommand(productId, 3), CancellationToken.None);

        var cart = await context.Carts.Include(c => c.Items).SingleAsync();
        Assert.Equal("active", cart.Status);
        Assert.Equal("web", cart.Channel);
        var line = Assert.Single(cart.Items);
        Assert.Equal(productId, line.ProductId);
        Assert.Equal(3, line.Quantity);
        Assert.Equal(250m, line.UnitPrice);
    }

    [Fact]
    public async Task RepeatAdd_AccumulatesQuantityOnTheSameLine()
    {
        var (context, user, productId) = await Seed(available: 10);
        var handler = new AddCartItemCommandHandler(context, user);

        await handler.Handle(new AddCartItemCommand(productId, 2), CancellationToken.None);
        await handler.Handle(new AddCartItemCommand(productId, 4), CancellationToken.None);

        var cart = await context.Carts.Include(c => c.Items).SingleAsync();
        var line = Assert.Single(cart.Items);
        Assert.Equal(6, line.Quantity);
    }

    [Fact]
    public async Task ProductWithNoUnits_ThrowsValidationExceptionAndAddsNothing()
    {
        var (context, user, productId) = await Seed(available: 0);
        var handler = new AddCartItemCommandHandler(context, user);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new AddCartItemCommand(productId, 1), CancellationToken.None));

        Assert.Contains("no está disponible", ex.Errors["Quantity"][0]);
        Assert.Empty(context.CartItems);
    }

    [Fact]
    public async Task QuantityBeyondAvailable_ThrowsValidationException()
    {
        var (context, user, productId) = await Seed(available: 2);
        var handler = new AddCartItemCommandHandler(context, user);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new AddCartItemCommand(productId, 5), CancellationToken.None));

        Assert.Contains("Solo quedan 2", ex.Errors["Quantity"][0]);
    }

    [Fact]
    public async Task AccumulatedQuantityBeyondAvailable_ThrowsValidationException()
    {
        var (context, user, productId) = await Seed(available: 3);
        var handler = new AddCartItemCommandHandler(context, user);

        await handler.Handle(new AddCartItemCommand(productId, 2), CancellationToken.None);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new AddCartItemCommand(productId, 2), CancellationToken.None));
    }
}
