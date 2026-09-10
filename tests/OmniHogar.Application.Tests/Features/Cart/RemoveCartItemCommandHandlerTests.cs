using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.Cart;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using CartEntity = OmniHogar.Domain.Entities.Cart;

namespace OmniHogar.Application.Tests.Features.Cart;

public class RemoveCartItemCommandHandlerTests
{
    private static User Customer(Guid id) =>
        new() { Id = id, UserType = UserType.customer, Email = $"{id:N}@x.test", FirstName = "C", LastName = "X", PasswordHash = "h" };

    private static async Task<(InMemoryApplicationDbContext Context, FakeCurrentUserService User, Guid KeptProductId, Guid RemovedProductId)> SeedCartWithTwoLines()
    {
        var context = InMemoryApplicationDbContext.Create();
        var userId = Guid.NewGuid();
        var kept = Guid.NewGuid();
        var removed = Guid.NewGuid();

        context.Users.Add(Customer(userId));
        context.Products.AddRange(
            new Product { Id = kept, Sku = "SKU-K", Name = "Mesa", Price = 300m, Status = "active" },
            new Product { Id = removed, Sku = "SKU-R", Name = "Silla", Price = 100m, Status = "active" });
        context.Carts.Add(new CartEntity
        {
            UserId = userId,
            Channel = "web",
            Status = "active",
            Items =
            {
                new CartItem { ProductId = kept, Quantity = 1, UnitPrice = 300m },
                new CartItem { ProductId = removed, Quantity = 2, UnitPrice = 100m },
            },
        });
        await context.SaveChangesAsync(CancellationToken.None);
        return (context, new FakeCurrentUserService { UserId = userId.ToString() }, kept, removed);
    }

    [Fact]
    public async Task RemovesOnlyTheTargetLine()
    {
        var (context, user, kept, removed) = await SeedCartWithTwoLines();
        var handler = new RemoveCartItemCommandHandler(context, user);

        await handler.Handle(new RemoveCartItemCommand(removed), CancellationToken.None);

        var remaining = await context.CartItems.ToListAsync();
        var line = Assert.Single(remaining);
        Assert.Equal(kept, line.ProductId);
    }

    [Fact]
    public async Task LineNotInCart_IsANoOp()
    {
        var (context, user, _, _) = await SeedCartWithTwoLines();
        var handler = new RemoveCartItemCommandHandler(context, user);

        await handler.Handle(new RemoveCartItemCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(2, await context.CartItems.CountAsync());
    }
}
