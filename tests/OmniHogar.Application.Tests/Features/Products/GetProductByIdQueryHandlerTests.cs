using OmniHogar.Application.Features.Products;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Tests.Features.Products;

public class GetProductByIdQueryHandlerTests
{
    private static Product SampleProduct() => new()
    {
        Sku = "SKU-001",
        Name = "Aspiradora Robot",
        Description = "Aspiradora robot con mapeo automático.",
        Price = 899_900m,
    };

    [Fact]
    public async Task ProductWithStock_ReturnsAvailabilityAndDetails()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var product = SampleProduct();
        var facility = new Facility { Name = "Bodega Central", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" };
        context.Products.Add(product);
        context.Facilities.Add(facility);
        context.Inventory.Add(new Inventory { ProductId = product.Id, FacilityId = facility.Id, AvailableQuantity = 12 });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProductByIdQueryHandler(context);
        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        Assert.Equal(product.Name, result.Name);
        Assert.Equal(product.Description, result.Description);
        Assert.Equal(product.Price, result.Price);
        Assert.Equal(12, result.AvailableQuantity);
        Assert.True(result.InStock);
    }

    [Fact]
    public async Task ProductWithNoInventoryRows_IsReportedAsNotAvailable()
    {
        // Crit. 3: sin unidades disponibles, el detalle debe indicar que no está disponible.
        await using var context = InMemoryApplicationDbContext.Create();
        var product = SampleProduct();
        context.Products.Add(product);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProductByIdQueryHandler(context);
        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        Assert.Equal(0, result.AvailableQuantity);
        Assert.False(result.InStock);
    }

    [Fact]
    public async Task NonexistentProduct_ThrowsNotFoundException()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var handler = new GetProductByIdQueryHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetProductByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
