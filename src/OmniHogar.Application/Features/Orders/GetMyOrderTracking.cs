using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Features.Orders;

/// <summary>
/// Tracking (current status + status trail) of one of the authenticated customer's own orders
/// (HU-14 seguimiento). Reads fresh from the database on every call, so a status change made by
/// staff shows up on the customer's next query. An order that doesn't exist, or belongs to
/// someone else, reports as "no fue encontrado".
/// </summary>
public record GetMyOrderTrackingQuery(Guid Id) : IRequest<OrderTrackingDto>;

public class GetMyOrderTrackingQueryHandler : IRequestHandler<GetMyOrderTrackingQuery, OrderTrackingDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyOrderTrackingQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<OrderTrackingDto> Handle(GetMyOrderTrackingQuery request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUser.UserId!);

        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == request.Id && o.UserId == userId, cancellationToken)
            ?? throw NotFoundException.PedidoNoEncontrado(request.Id);

        var history = order.StatusHistory
            .OrderBy(h => h.CreatedAt)
            .Select(h => new OrderTrackingEventDto { Status = h.NewStatus, OccurredAt = h.CreatedAt })
            .ToList();

        return new OrderTrackingDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            LastUpdatedAt = history.Count > 0 ? history[^1].OccurredAt : order.CreatedAt,
            History = history,
        };
    }
}
