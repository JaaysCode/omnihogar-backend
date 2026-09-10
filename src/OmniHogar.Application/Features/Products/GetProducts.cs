using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Features.Products;

/// <summary>Public catalog query (HU-4) — only active/available products.</summary>
public record GetProductsQuery : IRequest<List<ProductDto>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Hand-rolled projection (instead of ProjectTo) so each row can carry its cross-facility
        // availability (HU-05 crit. 1/4) via a correlated sum over Inventory — Product has no
        // Inventory navigation to map through AutoMapper.
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Status == "active")
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
