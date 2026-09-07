using OmniHogar.Application.Features.Products;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Tests.Features.Products;

public class GetProductStockQueryTests
{
    private static Product SampleProduct() => new()
    {
        Sku = "SKU-001",
        Name = "Aspiradora Robot",
        Price = 899_900m,
    };

    [Fact]
    public async Task ProductWithStock_ReturnsTotalAvailableUnits()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var product = SampleProduct();
        var facility = new Facility { Name = "Bodega Central", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" };
        context.Products.Add(product);
        context.Facilities.Add(facility);
        context.Inventory.Add(new Inventory { ProductId = product.Id, FacilityId = facility.Id, AvailableQuantity = 12 });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProductStockQueryHandler(context);
        var result = await handler.Handle(new GetProductStockQuery(product.Id), CancellationToken.None);

        Assert.Equal(12, result.TotalAvailable);
        Assert.True(result.InStock);
        Assert.Equal(product.Sku, result.Sku);
        Assert.Equal(product.Name, result.ProductName);
    }

    [Fact]
    public async Task ProductInMultipleFacilities_ReturnsPerFacilityBreakdown()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var product = SampleProduct();
        var warehouse = new Facility { Name = "Bodega Norte", Type = FacilityType.WAREHOUSE, Address = "Calle 2", City = "Medellín" };
        var store = new Facility { Name = "Tienda Centro", Type = FacilityType.POS, Address = "Calle 3", City = "Cali" };
        context.Products.Add(product);
        context.Facilities.AddRange(warehouse, store);
        context.Inventory.AddRange(
            new Inventory { ProductId = product.Id, FacilityId = warehouse.Id, AvailableQuantity = 20 },
            new Inventory { ProductId = product.Id, FacilityId = store.Id, AvailableQuantity = 5 });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProductStockQueryHandler(context);
        var result = await handler.Handle(new GetProductStockQuery(product.Id), CancellationToken.None);

        Assert.Equal(25, result.TotalAvailable);
        Assert.Equal(2, result.Facilities.Count);
        Assert.Contains(result.Facilities, f => f.FacilityId == warehouse.Id && f.AvailableQuantity == 20 && f.FacilityType == FacilityType.WAREHOUSE);
        Assert.Contains(result.Facilities, f => f.FacilityId == store.Id && f.AvailableQuantity == 5 && f.FacilityType == FacilityType.POS);
    }

    [Fact]
    public async Task ProductWithNoInventoryRows_IsReportedAsOutOfStock()
    {
        // No stock: product exists but has never been stocked anywhere (zero inventory rows).
        await using var context = InMemoryApplicationDbContext.Create();
        var product = SampleProduct();
        context.Products.Add(product);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProductStockQueryHandler(context);
        var result = await handler.Handle(new GetProductStockQuery(product.Id), CancellationToken.None);

        Assert.False(result.InStock);
        Assert.Equal(0, result.TotalAvailable);
        Assert.Empty(result.Facilities);
    }

    [Fact]
    public async Task ProductWithAllFacilitiesAtZero_IsReportedAsOutOfStock()
    {
        // No stock: product has inventory rows, but every facility is depleted to 0.
        await using var context = InMemoryApplicationDbContext.Create();
        var product = SampleProduct();
        var facilityA = new Facility { Name = "Bodega Sur", Type = FacilityType.WAREHOUSE, Address = "Calle 4", City = "Bogotá" };
        var facilityB = new Facility { Name = "Tienda Norte", Type = FacilityType.POS, Address = "Calle 5", City = "Bogotá" };
        context.Products.Add(product);
        context.Facilities.AddRange(facilityA, facilityB);
        context.Inventory.AddRange(
            new Inventory { ProductId = product.Id, FacilityId = facilityA.Id, AvailableQuantity = 0 },
            new Inventory { ProductId = product.Id, FacilityId = facilityB.Id, AvailableQuantity = 0 });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProductStockQueryHandler(context);
        var result = await handler.Handle(new GetProductStockQuery(product.Id), CancellationToken.None);

        Assert.False(result.InStock);
        Assert.Equal(0, result.TotalAvailable);
        Assert.Equal(2, result.Facilities.Count);
        Assert.All(result.Facilities, f => Assert.Equal(0, f.AvailableQuantity));
    }

    [Fact]
    public async Task NonexistentProduct_ThrowsNotFoundException()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var handler = new GetProductStockQueryHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetProductStockQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
