using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Features.Products;

/// <summary>
/// Available-units lookup for several products at once, broken down by facility — same shape as
/// <see cref="GetProductStockQuery"/> but batched, so admin list/inventory pages can fill a whole
/// table's Stock column with one round trip instead of fanning out one request per row. Ids that
/// don't match a product are silently omitted from the result rather than failing the batch.
/// </summary>
public record GetProductsStockQuery(IReadOnlyList<Guid> ProductIds) : IRequest<List<ProductStockDto>>;

public class GetProductsStockQueryHandler : IRequestHandler<GetProductsStockQuery, List<ProductStockDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductsStockQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductStockDto>> Handle(GetProductsStockQuery request, CancellationToken cancellationToken)
    {
        var ids = request.ProductIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var products = await _context.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var facilitiesByProduct = await _context.Inventory
            .AsNoTracking()
            .Where(i => ids.Contains(i.ProductId))
            .OrderBy(i => i.Facility.Name)
            .Select(i => new
            {
                i.ProductId,
                Stock = new FacilityStockDto
                {
                    FacilityId = i.FacilityId,
                    FacilityName = i.Facility.Name,
                    FacilityType = i.Facility.Type,
                    City = i.Facility.City,
                    AvailableQuantity = i.AvailableQuantity,
                },
            })
            .ToListAsync(cancellationToken);

        var facilitiesLookup = facilitiesByProduct
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Stock).ToList());

        return products
            .Select(product =>
            {
                var facilities = facilitiesLookup.TryGetValue(product.Id, out var value) ? value : [];
                var totalAvailable = facilities.Sum(f => f.AvailableQuantity);

                return new ProductStockDto
                {
                    ProductId = product.Id,
                    Sku = product.Sku,
                    ProductName = product.Name,
                    TotalAvailable = totalAvailable,
                    InStock = totalAvailable > 0,
                    Facilities = facilities,
                };
            })
            .ToList();
    }
}
