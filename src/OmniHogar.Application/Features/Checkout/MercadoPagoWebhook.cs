using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Features.Checkout;

/// <summary>
/// Backup reconciliation path for Mercado Pago's webhook (HU-09) — the buyer may close the tab
/// before the redirect-back confirmation runs. Never throws: any failure here is swallowed (the
/// controller always answers Mercado Pago with 204, per their integration guidance) since the
/// redirect-based <see cref="GetCheckoutStatusQuery"/> is the primary confirmation path.
/// </summary>
public record HandleMercadoPagoWebhookCommand(string? Type, string? PaymentId) : IRequest;

public class HandleMercadoPagoWebhookCommandHandler : IRequestHandler<HandleMercadoPagoWebhookCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IMercadoPagoClient _mercadoPago;

    public HandleMercadoPagoWebhookCommandHandler(IApplicationDbContext context, IMercadoPagoClient mercadoPago)
    {
        _context = context;
        _mercadoPago = mercadoPago;
    }

    public async Task Handle(HandleMercadoPagoWebhookCommand request, CancellationToken cancellationToken)
    {
        if (request.Type != "payment" || string.IsNullOrWhiteSpace(request.PaymentId))
        {
            return;
        }

        try
        {
            var mercadoPagoPayment = await _mercadoPago.GetPaymentAsync(request.PaymentId, cancellationToken);
            if (!Guid.TryParse(mercadoPagoPayment.ExternalReference, out var orderId))
            {
                return;
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            var payment = order is null ? null : await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id, cancellationToken);
            if (order is null || payment is null)
            {
                return;
            }

            await PaymentReconciler.ApplyAsync(_context, order, payment, mercadoPagoPayment.Status, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Swallowed on purpose — see class doc. Nothing to inform the caller of; the
            // controller always returns 204 to Mercado Pago regardless.
        }
    }
}
