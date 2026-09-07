using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using OmniHogar.Application.Common.Mappings;
using OmniHogar.Application.Features.Products;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Tests.Features.Products;

public class GetCategoriesQueryTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task Categories_AreReturnedAlphabetically()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        context.ProductCategories.AddRange(
            new ProductCategory { Name = "Jardín y Exteriores" },
            new ProductCategory { Name = "Baño" },
            new ProductCategory { Name = "Cocina" });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetCategoriesQueryHandler(context, CreateMapper());
        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { "Baño", "Cocina", "Jardín y Exteriores" }, result.Select(c => c.Name));
    }

    [Fact]
    public async Task NoCategories_ReturnsEmptyList()
    {
        await using var context = InMemoryApplicationDbContext.Create();

        var handler = new GetCategoriesQueryHandler(context, CreateMapper());
        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}
