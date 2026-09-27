using OmniHogar.Application.Features.Products;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Products;

public class SearchProductsQueryTests
{
    [Fact]
    public async Task SearchByPartialName_IsCaseInsensitive_ReturnsMatches()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        context.Products.AddRange(
            new Product { Sku = "SKU-001", Name = "Aspiradora Robot", Price = 899_900m },
            new Product { Sku = "SKU-002", Name = "Licuadora Industrial", Price = 199_900m });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new SearchProductsQueryHandler(context);
        var result = await handler.Handle(new SearchProductsQuery("aspira", null), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Aspiradora Robot", result[0].Name);
    }

    [Fact]
    public async Task SearchByCategory_ReturnsOnlyProductsInThatCategory()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var cocina = new ProductCategory { Name = "Cocina" };
        var limpieza = new ProductCategory { Name = "Limpieza" };
        context.ProductCategories.AddRange(cocina, limpieza);
        context.Products.AddRange(
            new Product { Sku = "SKU-001", Name = "Licuadora", Price = 199_900m, CategoryId = cocina.Id },
            new Product { Sku = "SKU-002", Name = "Aspiradora", Price = 899_900m, CategoryId = limpieza.Id });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new SearchProductsQueryHandler(context);
        var result = await handler.Handle(new SearchProductsQuery(null, cocina.Id), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Licuadora", result[0].Name);
    }

    [Fact]
    public async Task NoMatches_ReturnsEmptyList()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        context.Products.Add(new Product { Sku = "SKU-001", Name = "Aspiradora Robot", Price = 899_900m });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new SearchProductsQueryHandler(context);
        var result = await handler.Handle(new SearchProductsQuery("inexistente", null), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task DiscontinuedProducts_AreExcludedFromSearch()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        context.Products.Add(new Product { Sku = "SKU-001", Name = "Aspiradora Robot", Price = 899_900m, Status = "discontinued" });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new SearchProductsQueryHandler(context);
        var result = await handler.Handle(new SearchProductsQuery("aspiradora", null), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task NameAndCategoryCombined_MatchesOnlyProductsSatisfyingBoth()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var cocina = new ProductCategory { Name = "Cocina" };
        var limpieza = new ProductCategory { Name = "Limpieza" };
        context.ProductCategories.AddRange(cocina, limpieza);
        context.Products.AddRange(
            new Product { Sku = "SKU-001", Name = "Aspiradora de Mano", Price = 199_900m, CategoryId = cocina.Id },
            new Product { Sku = "SKU-002", Name = "Aspiradora Robot", Price = 899_900m, CategoryId = limpieza.Id });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new SearchProductsQueryHandler(context);
        var result = await handler.Handle(new SearchProductsQuery("aspiradora", limpieza.Id), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Aspiradora Robot", result[0].Name);
    }

    [Fact]
    public async Task WhitespaceOnlyName_IsIgnored_ReturnsAllActiveProducts()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        context.Products.AddRange(
            new Product { Sku = "SKU-001", Name = "Aspiradora Robot", Price = 899_900m },
            new Product { Sku = "SKU-002", Name = "Licuadora Industrial", Price = 199_900m });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new SearchProductsQueryHandler(context);
        var result = await handler.Handle(new SearchProductsQuery("   ", null), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task MatchingProductWithStock_ReportsAvailableQuantityAndInStock()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var product = new Product { Sku = "SKU-001", Name = "Aspiradora Robot", Price = 899_900m };
        var facility = new Facility { Name = "Bodega Central", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" };
        context.Products.Add(product);
        context.Facilities.Add(facility);
        context.Inventory.Add(new Inventory { ProductId = product.Id, FacilityId = facility.Id, AvailableQuantity = 7 });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new SearchProductsQueryHandler(context);
        var result = await handler.Handle(new SearchProductsQuery("aspiradora", null), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(7, result[0].AvailableQuantity);
        Assert.True(result[0].InStock);
    }

    [Fact]
    public async Task MatchingProductWithNoInventoryRows_ReportsOutOfStock()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        context.Products.Add(new Product { Sku = "SKU-001", Name = "Aspiradora Robot", Price = 899_900m });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new SearchProductsQueryHandler(context);
        var result = await handler.Handle(new SearchProductsQuery("aspiradora", null), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(0, result[0].AvailableQuantity);
        Assert.False(result[0].InStock);
    }
}
