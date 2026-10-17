using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Features.InventoryTransfers;

/// <summary>Every warehouse and point of sale, by name — feeds the transfer origin/destination pickers.</summary>
public record GetFacilitiesQuery : IRequest<List<FacilityDto>>;

public class GetFacilitiesQueryHandler : IRequestHandler<GetFacilitiesQuery, List<FacilityDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFacilitiesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FacilityDto>> Handle(GetFacilitiesQuery request, CancellationToken cancellationToken)
    {
        var facilities = await _context.Facilities
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

        return facilities
            .Select(f => new FacilityDto { Id = f.Id, Name = f.Name, Type = f.Type.ToString(), City = f.City })
            .ToList();
    }
}
