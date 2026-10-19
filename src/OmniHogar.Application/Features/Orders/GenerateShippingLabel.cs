using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Features.Orders;

/// <summary>
/// Generates the shipping guide for an order that is ready for dispatch (HU-25). "Ready" means
/// <c>packed</c>; any earlier status is rejected so a guide never exists for an order that has not
/// been prepared (crit. 3).
/// </summary>
public record GenerateShippingLabelCommand(Guid OrderId) : IRequest<ShippingLabelDto>;

public class GenerateShippingLabelCommandHandler : IRequestHandler<GenerateShippingLabelCommand, ShippingLabelDto>
{
    private const string DefaultCarrier = "OmniHogar Logística";

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GenerateShippingLabelCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ShippingLabelDto> Handle(GenerateShippingLabelCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.ShippingAddress)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Dispatch)
                .ThenInclude(d => d!.ShippingLabels)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw NotFoundException.Pedido(request.OrderId);

        if (order.Status != "packed")
        {
            throw Fail("El pedido aún no está preparado para despacho.");
        }

        if (order.ShippingAddress is null)
        {
            throw Fail("El pedido no tiene dirección de entrega.");
        }

        if (order.Dispatch?.ShippingLabels.Count > 0)
        {
            throw Fail("El pedido ya tiene una guía de envío.");
        }

        var dispatch = order.Dispatch;
        if (dispatch is null)
        {
            dispatch = new Dispatch { OrderId = order.Id, SourceFacilityId = order.FacilityId, StartedAt = DateTime.UtcNow };
            _context.Dispatches.Add(dispatch);
            order.Dispatch = dispatch;
        }

        dispatch.Status = "packed";
        dispatch.PackedAt ??= DateTime.UtcNow;
        dispatch.HandlerId = Guid.Parse(_currentUser.UserId!);

        var label = new ShippingLabel
        {
            DispatchId = dispatch.Id,
            Carrier = DefaultCarrier,
            TrackingNumber = $"OH-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}",
        };
        _context.ShippingLabels.Add(label);

        await _context.SaveChangesAsync(cancellationToken);

        return ShippingLabelDto.From(order, label);
    }

    private static ValidationException Fail(string message) =>
        new(new Dictionary<string, string[]> { ["orderId"] = [message] });
}
