using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.Cart;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;
using CartEntity = OmniHogar.Domain.Entities.Cart;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Tests.Features.Cart;

public class UpdateCartItemQuantityCommandHandlerTests
{
    private static User Customer(Guid id) =>
        new() { Id = id, UserType = UserType.customer, Email = $"{id:N}@x.test", FirstName = "C", LastName = "X", PasswordHash = "h" };

    private static async Task<(InMemoryApplicationDbContext Context, FakeCurrentUserService User, Guid ProductId)> SeedCartWithLine(int available, int lineQuantity)
    {
        var context = InMemoryApplicationDbContext.Create();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var facility = new Facility { Name = "Bodega", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" };

        context.Users.Add(Customer(userId));
        context.Products.Add(new Product { Id = productId, Sku = "SKU-1", Name = "Silla", Price = 100m, Status = "active" });
        context.Facilities.Add(facility);
        context.Inventory.Add(new Inventory { ProductId = productId, FacilityId = facility.Id, AvailableQuantity = available });
        context.Carts.Add(new CartEntity
        {
            UserId = userId,
            Channel = "web",
            Status = "active",
            Items = { new CartItem { ProductId = productId, Quantity = lineQuantity, UnitPrice = 100m } },
        });
        await context.SaveChangesAsync(CancellationToken.None);
        return (context, new FakeCurrentUserService { UserId = userId.ToString() }, productId);
    }

    [Fact]
    public async Task SetsTheLineQuantity()
    {
        var (context, user, productId) = await SeedCartWithLine(available: 10, lineQuantity: 2);
        var handler = new UpdateCartItemQuantityCommandHandler(context, user);

        await handler.Handle(new UpdateCartItemQuantityCommand(productId, 5), CancellationToken.None);

        var line = await context.CartItems.SingleAsync(ci => ci.ProductId == productId);
        Assert.Equal(5, line.Quantity);
    }

    [Fact]
    public async Task QuantityBeyondAvailable_ThrowsValidationException()
    {
        var (context, user, productId) = await SeedCartWithLine(available: 4, lineQuantity: 2);
        var handler = new UpdateCartItemQuantityCommandHandler(context, user);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new UpdateCartItemQuantityCommand(productId, 9), CancellationToken.None));
    }

    [Fact]
    public async Task LineNotInCart_ThrowsNotFound()
    {
        var (context, user, _) = await SeedCartWithLine(available: 10, lineQuantity: 2);
        var handler = new UpdateCartItemQuantityCommandHandler(context, user);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new UpdateCartItemQuantityCommand(Guid.NewGuid(), 1), CancellationToken.None));
    }
}
