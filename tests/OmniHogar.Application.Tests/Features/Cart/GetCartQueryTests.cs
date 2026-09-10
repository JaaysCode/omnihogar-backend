using OmniHogar.Application.Features.Cart;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using CartEntity = OmniHogar.Domain.Entities.Cart;

namespace OmniHogar.Application.Tests.Features.Cart;

public class GetCartQueryTests
{
    private static User Customer(Guid id) =>
        new() { Id = id, UserType = UserType.customer, Email = $"{id:N}@x.test", FirstName = "C", LastName = "X", PasswordHash = "h" };

    [Fact]
    public async Task NoCartYet_ReturnsEmptyCart()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var user = new FakeCurrentUserService { UserId = Guid.NewGuid().ToString() };
        var handler = new GetCartQueryHandler(context, user);

        var result = await handler.Handle(new GetCartQuery(), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0m, result.Total);
        Assert.Equal(0, result.ItemCount);
    }

    [Fact]
    public async Task ActiveCart_ComputesLineAndCartTotalsAndAvailability()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var userId = Guid.NewGuid();
        var chair = Guid.NewGuid();
        var table = Guid.NewGuid();
        var facility = new Facility { Name = "Bodega", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" };

        context.Users.Add(Customer(userId));
        context.Products.AddRange(
            new Product { Id = chair, Sku = "SKU-C", Name = "Silla", Price = 100m, Status = "active" },
            new Product { Id = table, Sku = "SKU-T", Name = "Mesa", Price = 300m, Status = "active" });
        context.Facilities.Add(facility);
        context.Inventory.AddRange(
            new Inventory { ProductId = chair, FacilityId = facility.Id, AvailableQuantity = 7 },
            new Inventory { ProductId = table, FacilityId = facility.Id, AvailableQuantity = 4 });
        context.Carts.Add(new CartEntity
        {
            UserId = userId,
            Channel = "web",
            Status = "active",
            Items =
            {
                new CartItem { ProductId = chair, Quantity = 2, UnitPrice = 100m },
                new CartItem { ProductId = table, Quantity = 1, UnitPrice = 300m },
            },
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetCartQueryHandler(context, new FakeCurrentUserService { UserId = userId.ToString() });
        var result = await handler.Handle(new GetCartQuery(), CancellationToken.None);

        Assert.Equal(3, result.ItemCount);
        Assert.Equal(500m, result.Subtotal);
        Assert.Equal(500m, result.Total);

        var chairLine = Assert.Single(result.Items, i => i.ProductId == chair);
        Assert.Equal(200m, chairLine.Subtotal);
        Assert.Equal(7, chairLine.AvailableQuantity);
        Assert.Equal("Silla", chairLine.Name);

        var tableLine = Assert.Single(result.Items, i => i.ProductId == table);
        Assert.Equal(300m, tableLine.Subtotal);
        Assert.Equal(4, tableLine.AvailableQuantity);
    }
}
