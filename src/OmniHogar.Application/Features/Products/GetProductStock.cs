using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Features.Products;

/// <summary>Available-units lookup for one product, broken down by facility (warehouse/store).</summary>
public record GetProductStockQuery(Guid ProductId) : IRequest<ProductStockDto>;

public class GetProductStockQueryHandler : IRequestHandler<GetProductStockQuery, ProductStockDto>
{
    private readonly IApplicationDbContext _context;

    public GetProductStockQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductStockDto> Handle(GetProductStockQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException(nameof(Domain.Entities.Product), request.ProductId);
        }

        var facilities = await _context.Inventory
            .AsNoTracking()
            .Where(i => i.ProductId == product.Id)
            .OrderBy(i => i.Facility.Name)
            .Select(i => new FacilityStockDto
            {
                FacilityId = i.FacilityId,
                FacilityName = i.Facility.Name,
                FacilityType = i.Facility.Type,
                City = i.Facility.City,
                AvailableQuantity = i.AvailableQuantity,
            })
            .ToListAsync(cancellationToken);

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
    }
}
