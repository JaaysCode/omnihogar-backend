using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Features.Checkout;

/// <summary>
/// Status of a checkout's order/payment (HU-09 crit. 1/2), for the /checkout/result page. When
/// a <paramref name="PaymentId"/> is given and the payment is still "pending", actively verifies
/// it against Mercado Pago first — this is the primary confirmation path (the webhook is the
/// backup for when the buyer never comes back). Never throws on a gateway failure — reports
/// <see cref="CheckoutStatusDto.GatewayUnavailable"/> instead (HU-09 crit. 3), the order stays
/// exactly as it was.
/// </summary>
public record GetCheckoutStatusQuery(Guid OrderId, string? PaymentId) : IRequest<CheckoutStatusDto>;

public class GetCheckoutStatusQueryHandler : IRequestHandler<GetCheckoutStatusQuery, CheckoutStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMercadoPagoClient _mercadoPago;

    public GetCheckoutStatusQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, IMercadoPagoClient mercadoPago)
    {
        _context = context;
        _currentUser = currentUser;
        _mercadoPago = mercadoPago;
    }

    public async Task<CheckoutStatusDto> Handle(GetCheckoutStatusQuery request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUser.UserId!);

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId, cancellationToken)
            ?? throw NotFoundException.Pedido(request.OrderId);

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id, cancellationToken);

        var gatewayUnavailable = false;

        if (payment is not null && payment.Status == "pending" && !string.IsNullOrWhiteSpace(request.PaymentId))
        {
            try
            {
                var mercadoPagoPayment = await _mercadoPago.GetPaymentAsync(request.PaymentId, cancellationToken);
                await PaymentReconciler.ApplyAsync(_context, order, payment, mercadoPagoPayment.Status, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception)
            {
                gatewayUnavailable = true;
            }
        }

        return new CheckoutStatusDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            OrderStatus = order.Status,
            PaymentStatus = payment?.Status ?? "pending",
            Total = order.Total,
            GatewayUnavailable = gatewayUnavailable,
        };
    }
}
