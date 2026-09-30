using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Exceptions;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Features.Checkout;

/// <summary>
/// Generates a fresh Stripe Checkout session for an order that's still awaiting payment —
/// after a rejection or a communication error (HU-09 crit. 2/3 "gestión correspondiente"),
/// without touching the cart or creating a new order.
/// </summary>
public record RetryCheckoutPaymentCommand(Guid OrderId) : IRequest<CheckoutDto>;

public class RetryCheckoutPaymentCommandHandler : IRequestHandler<RetryCheckoutPaymentCommand, CheckoutDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPaymentGatewayClient _paymentGateway;
    private readonly IPaymentGatewayUrls _urls;

    public RetryCheckoutPaymentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IPaymentGatewayClient paymentGateway,
        IPaymentGatewayUrls urls)
    {
        _context = context;
        _currentUser = currentUser;
        _paymentGateway = paymentGateway;
        _urls = urls;
    }

    public async Task<CheckoutDto> Handle(RetryCheckoutPaymentCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUser.UserId!);

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId, cancellationToken)
            ?? throw NotFoundException.Pedido(request.OrderId);

        if (order.Status == "payment_approved")
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Order"] = ["Este pedido ya fue pagado."],
            });
        }

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id, cancellationToken)
            ?? throw NotFoundException.Pedido(request.OrderId);

        order.Status = "pending_payment";
        payment.Status = "pending";
        payment.ConfirmedAt = null;
        await _context.SaveChangesAsync(cancellationToken);

        string initPoint;
        try
        {
            var resultUrl = $"{_urls.FrontendBaseUrl}/checkout/result?order={order.Id}";
            var preference = await _paymentGateway.CreatePreferenceAsync(new PaymentGatewayPreferenceRequest
            {
                Title = $"Pedido {order.OrderNumber}",
                UnitPrice = order.Total,
                ExternalReference = order.Id.ToString(),
                PayerEmail = _currentUser.Email,
                SuccessUrl = resultUrl,
                FailureUrl = resultUrl,
                PendingUrl = resultUrl,
                NotificationUrl = $"{_urls.BackendPublicBaseUrl}/api/checkout/webhook",
            }, cancellationToken);

            initPoint = preference.InitPoint;
        }
        catch (Exception)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Gateway"] = ["No fue posible conectar con la pasarela de pago. Tu pedido sigue guardado; puedes intentarlo de nuevo."],
            });
        }

        return new CheckoutDto { OrderId = order.Id, OrderNumber = order.OrderNumber, InitPoint = initPoint };
    }
}
