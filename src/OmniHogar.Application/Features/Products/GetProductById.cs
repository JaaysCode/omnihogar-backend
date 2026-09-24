using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Features.Products;

/// <summary>Public product detail (HU-18) — anonymous, includes availability.</summary>
public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IApplicationDbContext _context;

    public GetProductByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        // Hand-rolled projection (same as GetProductsQuery) instead of ProjectTo/AutoMapper — Product
        // has no Inventory navigation, so availability (HU-18 crit. 2/3) needs a correlated subquery.
        var product = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            throw NotFoundException.Producto(request.Id);
        }

        return product;
    }
}
