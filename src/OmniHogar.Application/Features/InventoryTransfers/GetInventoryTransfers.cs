using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Features.InventoryTransfers;

/// <summary>Inventory transfers, newest first, optionally filtered to one status.</summary>
public record GetInventoryTransfersQuery(string? Status) : IRequest<List<InventoryTransferDto>>;

public class GetInventoryTransfersQueryHandler : IRequestHandler<GetInventoryTransfersQuery, List<InventoryTransferDto>>
{
    private readonly IApplicationDbContext _context;

    public GetInventoryTransfersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<InventoryTransferDto>> Handle(GetInventoryTransfersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.InventoryTransfers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(t => t.Status == request.Status);
        }

        var transfers = await query
            .Include(t => t.Product)
            .Include(t => t.SourceFacility)
            .Include(t => t.DestinationFacility)
            .OrderByDescending(t => t.RequestedAt)
            .ToListAsync(cancellationToken);

        return transfers.Select(t => t.ToDto()).ToList();
    }
}
