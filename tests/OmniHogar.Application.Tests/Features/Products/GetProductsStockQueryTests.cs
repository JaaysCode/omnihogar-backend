using OmniHogar.Application.Features.Products;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Products;

public class GetProductsStockQueryTests
{
    private static Product SampleProduct(string sku, string name) => new()
    {
        Sku = sku,
        Name = name,
        Price = 100_000m,
    };

    [Fact]
    public async Task MultipleProducts_ReturnsOneEntryPerProductWithItsOwnTotal()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var productA = SampleProduct("SKU-A", "Aspiradora Robot");
        var productB = SampleProduct("SKU-B", "Licuadora");
        var facility = new Facility { Name = "Bodega Central", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" };
        context.Products.AddRange(productA, productB);
        context.Facilities.Add(facility);
        context.Inventory.AddRange(
            new Inventory { ProductId = productA.Id, FacilityId = facility.Id, AvailableQuantity = 12 },
            new Inventory { ProductId = productB.Id, FacilityId = facility.Id, AvailableQuantity = 0 });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProductsStockQueryHandler(context);
        var result = await handler.Handle(new GetProductsStockQuery([productA.Id, productB.Id]), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.ProductId == productA.Id && r.TotalAvailable == 12 && r.InStock);
        Assert.Contains(result, r => r.ProductId == productB.Id && r.TotalAvailable == 0 && !r.InStock);
    }

    [Fact]
    public async Task ProductWithNoInventoryRows_IsReportedAsOutOfStockWithEmptyFacilities()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var product = SampleProduct("SKU-C", "Ventilador");
        context.Products.Add(product);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProductsStockQueryHandler(context);
        var result = await handler.Handle(new GetProductsStockQuery([product.Id]), CancellationToken.None);

        var entry = Assert.Single(result);
        Assert.False(entry.InStock);
        Assert.Equal(0, entry.TotalAvailable);
        Assert.Empty(entry.Facilities);
    }

    [Fact]
    public async Task UnknownIds_AreSilentlyOmittedFromTheResult()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var handler = new GetProductsStockQueryHandler(context);

        var result = await handler.Handle(new GetProductsStockQuery([Guid.NewGuid(), Guid.NewGuid()]), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task EmptyIdList_ReturnsEmptyResultWithoutQuerying()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var handler = new GetProductsStockQueryHandler(context);

        var result = await handler.Handle(new GetProductsStockQuery([]), CancellationToken.None);

        Assert.Empty(result);
    }
}
