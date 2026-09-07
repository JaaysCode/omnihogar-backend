using OmniHogar.Application.Features.Products;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Products;

public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task WithInitialStock_CreatesInventoryRow_AndLogsMovement()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        context.Facilities.Add(new Facility { Name = "Bodega Central", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" });
        await context.SaveChangesAsync(CancellationToken.None);

        var currentUser = new FakeCurrentUserService();
        var handler = new CreateProductCommandHandler(context, currentUser);
        var productId = await handler.Handle(
            new CreateProductCommand("SKU-010", "Licuadora", null, null, 150_000m, null, 30),
            CancellationToken.None);

        var inventory = Assert.Single(context.Inventory);
        Assert.Equal(productId, inventory.ProductId);
        Assert.Equal(30, inventory.AvailableQuantity);

        var movement = Assert.Single(context.InventoryMovements);
        Assert.Equal("inbound", movement.Type);
        Assert.Equal(30, movement.Quantity);
        Assert.Equal("Stock inicial", movement.Reason);
    }

    [Fact]
    public async Task WithoutInitialStock_CreatesNoInventoryRow()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        context.Facilities.Add(new Facility { Name = "Bodega Central", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateProductCommandHandler(context, new FakeCurrentUserService());
        await handler.Handle(new CreateProductCommand("SKU-011", "Ventilador", null, null, 90_000m, null, null), CancellationToken.None);

        Assert.Empty(context.Inventory);
        Assert.Empty(context.InventoryMovements);
    }

    [Fact]
    public async Task ZeroInitialStock_CreatesNoInventoryRow()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        context.Facilities.Add(new Facility { Name = "Bodega Central", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateProductCommandHandler(context, new FakeCurrentUserService());
        await handler.Handle(new CreateProductCommand("SKU-012", "Plancha", null, null, 60_000m, null, 0), CancellationToken.None);

        Assert.Empty(context.Inventory);
    }

    [Fact]
    public async Task NoFacilityConfigured_StillCreatesProduct_ButSkipsInventoryRow()
    {
        await using var context = InMemoryApplicationDbContext.Create();

        var handler = new CreateProductCommandHandler(context, new FakeCurrentUserService());
        var productId = await handler.Handle(
            new CreateProductCommand("SKU-013", "Microondas", null, null, 300_000m, null, 10),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, productId);
        Assert.Empty(context.Inventory);
    }
}
