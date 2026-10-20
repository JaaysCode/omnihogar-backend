using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Features.Products;

/// <summary>
/// Low-stock alerts (HU-24), computed on the fly from the inventory: a product at a facility
/// alerts when it has a configured minimum (<c>MinimumStock &gt; 0</c> — 0 means "no minimum
/// configured") and its available units are equal to or below that level (crit. 1). Units above the
/// minimum never alert (crit. 3). Most urgent first.
/// </summary>
public record GetStockAlertsQuery : IRequest<List<StockAlertDto>>;

public class GetStockAlertsQueryHandler : IRequestHandler<GetStockAlertsQuery, List<StockAlertDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStockAlertsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<StockAlertDto>> Handle(GetStockAlertsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Inventory
            .AsNoTracking()
            .Where(i => i.MinimumStock > 0 && i.AvailableQuantity <= i.MinimumStock)
            .OrderBy(i => i.AvailableQuantity - i.MinimumStock)
            .ThenBy(i => i.Product.Name)
            .ThenBy(i => i.Facility.Name)
            .Select(i => new StockAlertDto(
                i.ProductId,
                i.Product.Sku,
                i.Product.Name,
                i.FacilityId,
                i.Facility.Name,
                i.AvailableQuantity,
                i.MinimumStock))
            .ToListAsync(cancellationToken);
    }
}
