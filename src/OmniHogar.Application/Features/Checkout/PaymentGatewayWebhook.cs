using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Features.Checkout;

/// <summary>
/// Backup reconciliation path for Stripe's webhook (HU-09) — the buyer may close the tab before
/// the redirect-back confirmation runs. Never throws: any failure here is swallowed (the
/// controller always answers 204, per Stripe's integration guidance) since the redirect-based
/// <see cref="GetCheckoutStatusQuery"/> is the primary confirmation path.
/// </summary>
public record HandlePaymentGatewayWebhookCommand(string? Type, string? PaymentId) : IRequest;

public class HandlePaymentGatewayWebhookCommandHandler : IRequestHandler<HandlePaymentGatewayWebhookCommand>
{
    private static readonly string[] RelevantEventTypes =
    [
        "checkout.session.completed",
        "checkout.session.async_payment_succeeded",
        "checkout.session.async_payment_failed",
        "checkout.session.expired",
    ];

    private readonly IApplicationDbContext _context;
    private readonly IPaymentGatewayClient _paymentGateway;

    public HandlePaymentGatewayWebhookCommandHandler(IApplicationDbContext context, IPaymentGatewayClient paymentGateway)
    {
        _context = context;
        _paymentGateway = paymentGateway;
    }

    public async Task Handle(HandlePaymentGatewayWebhookCommand request, CancellationToken cancellationToken)
    {
        if (request.Type is null || !RelevantEventTypes.Contains(request.Type) || string.IsNullOrWhiteSpace(request.PaymentId))
        {
            return;
        }

        try
        {
            var gatewayPayment = await _paymentGateway.GetPaymentAsync(request.PaymentId, cancellationToken);
            if (!Guid.TryParse(gatewayPayment.ExternalReference, out var orderId))
            {
                return;
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            var payment = order is null ? null : await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id, cancellationToken);
            if (order is null || payment is null)
            {
                return;
            }

            await PaymentReconciler.ApplyAsync(_context, order, payment, gatewayPayment.Status, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Swallowed on purpose — see class doc. Nothing to inform the caller of; the
            // controller always returns 204 to Stripe regardless.
        }
    }
}
