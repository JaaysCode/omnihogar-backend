using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Features.Products;

/// <summary>
/// Public catalog search (HU-XX) — active products filtered by name (partial, case-insensitive)
/// and/or category. Empty result list when nothing matches.
/// </summary>
public record SearchProductsQuery(string? Name, Guid? CategoryId) : IRequest<List<ProductDto>>;

public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, List<ProductDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductDto>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products.AsNoTracking().Where(p => p.Status == "active");

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var term = request.Name.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }

        if (request.CategoryId is not null)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId);
        }

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                CategoryId = p.CategoryId,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                Status = p.Status,
                AvailableQuantity =
                    _context.Inventory.Where(i => i.ProductId == p.Id).Sum(i => (int?)i.AvailableQuantity) ?? 0,
                InStock =
                    (_context.Inventory.Where(i => i.ProductId == p.Id).Sum(i => (int?)i.AvailableQuantity) ?? 0) > 0,
            })
            .ToListAsync(cancellationToken);
    }
}
