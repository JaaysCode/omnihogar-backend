using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Features.Orders;

/// <summary>Consult the shipping guide already generated for an order (HU-25 crit. 2).</summary>
public record GetShippingLabelQuery(Guid OrderId) : IRequest<ShippingLabelDto>;

public class GetShippingLabelQueryHandler : IRequestHandler<GetShippingLabelQuery, ShippingLabelDto>
{
    private readonly IApplicationDbContext _context;

    public GetShippingLabelQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShippingLabelDto> Handle(GetShippingLabelQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.ShippingAddress)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Dispatch)
                .ThenInclude(d => d!.ShippingLabels)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw NotFoundException.Pedido(request.OrderId);

        var label = order.Dispatch?.ShippingLabels
            .OrderByDescending(l => l.GeneratedAt)
            .FirstOrDefault()
            ?? throw NotFoundException.GuiaDeEnvio(request.OrderId);

        return ShippingLabelDto.From(order, label);
    }
}
