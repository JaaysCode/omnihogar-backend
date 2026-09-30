using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Features.Checkout;

/// <summary>
/// Status of a checkout's order/payment (HU-09 crit. 1/2), for the /checkout/result page. While
/// the payment is still "pending", actively verifies it against the gateway first — by
/// <paramref name="PaymentId"/> when given, else by the order's external_reference. The payment
/// must belong to this order. This is the primary confirmation path (the webhook is the
/// backup for when the buyer never comes back). Never throws on a gateway failure — reports
/// <see cref="CheckoutStatusDto.GatewayUnavailable"/> instead (HU-09 crit. 3), the order stays
/// exactly as it was.
/// </summary>
/// <param name="Cancelled">Set when the buyer bailed out of Stripe's hosted page (its "back to
/// merchant" link) instead of paying. Stripe never surfaces a "declined" state to us — the
/// session just stays open/retryable — so this is the only signal we get that the attempt is
/// dead; when set, the payment is marked rejected directly, without asking Stripe (which would
/// still report it as pending).</param>
public record GetCheckoutStatusQuery(Guid OrderId, string? PaymentId, bool Cancelled = false) : IRequest<CheckoutStatusDto>;

public class GetCheckoutStatusQueryHandler : IRequestHandler<GetCheckoutStatusQuery, CheckoutStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPaymentGatewayClient _paymentGateway;

    public GetCheckoutStatusQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, IPaymentGatewayClient paymentGateway)
    {
        _context = context;
        _currentUser = currentUser;
        _paymentGateway = paymentGateway;
    }

    public async Task<CheckoutStatusDto> Handle(GetCheckoutStatusQuery request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUser.UserId!);

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId, cancellationToken)
            ?? throw NotFoundException.Pedido(request.OrderId);

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id, cancellationToken);

        var gatewayUnavailable = false;

        if (payment is not null && payment.Status == "pending" && request.Cancelled)
        {
            // No gateway call needed (or possible, really — Stripe would still say the session is
            // open/pending): the buyer explicitly bailed out, so this attempt is done.
            await PaymentReconciler.ApplyAsync(_context, order, payment, "rejected", cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else if (payment is not null && payment.Status == "pending")
        {
            try
            {
                // Without a payment_id (e.g. the buyer lost the URL param) fall back to
                // searching by our external_reference.
                var gatewayPayment = !string.IsNullOrWhiteSpace(request.PaymentId)
                    ? await _paymentGateway.GetPaymentAsync(request.PaymentId, cancellationToken)
                    : await _paymentGateway.FindLatestPaymentAsync(order.Id.ToString(), cancellationToken);

                if (gatewayPayment is not null
                    && (gatewayPayment.ExternalReference is null || gatewayPayment.ExternalReference == order.Id.ToString()))
                {
                    await PaymentReconciler.ApplyAsync(_context, order, payment, gatewayPayment.Status, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                }
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
