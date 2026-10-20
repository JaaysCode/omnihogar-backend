using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Features.Orders;

/// <summary>
/// Payment receipt of one of the authenticated customer's own orders (HU-20). Derived on the fly
/// from the approved <c>Payment</c> and the order — nothing extra is stored, and an approved
/// payment never changes afterwards, so the receipt is stable. A payment that was not approved has
/// no receipt (crit. 3): the error reports the transaction's status instead.
/// </summary>
public record GetMyOrderReceiptQuery(Guid Id) : IRequest<OrderReceiptDto>;

public class GetMyOrderReceiptQueryHandler : IRequestHandler<GetMyOrderReceiptQuery, OrderReceiptDto>
{
    private static readonly Dictionary<string, string> PaymentStatusLabels = new()
    {
        ["pending"] = "Pendiente",
        ["rejected"] = "Rechazado",
        ["reversed"] = "Reversado",
    };

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyOrderReceiptQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<OrderReceiptDto> Handle(GetMyOrderReceiptQuery request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUser.UserId!);

        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Payments)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == request.Id && o.UserId == userId, cancellationToken)
            ?? throw NotFoundException.PedidoNoEncontrado(request.Id);

        var payment = order.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        if (payment is null)
        {
            throw Fail("El pedido no tiene un pago en línea registrado.");
        }

        if (payment.Status != "approved")
        {
            var status = PaymentStatusLabels.GetValueOrDefault(payment.Status, payment.Status);
            throw Fail($"El pago del pedido no fue aprobado (estado: {status}); no hay comprobante.");
        }

        return new OrderReceiptDto(
            $"CP-{order.OrderNumber}",
            order.Id,
            order.OrderNumber,
            payment.ConfirmedAt ?? payment.CreatedAt,
            payment.Amount,
            payment.PaymentMethod,
            payment.GatewayTransactionId,
            $"{order.User.FirstName} {order.User.LastName}",
            order.Items
                .Select(i => new OrderReceiptItemDto(i.Product.Name, i.Product.Sku, i.Quantity, i.UnitPrice, i.Subtotal))
                .ToList(),
            order.Subtotal,
            order.Total);
    }

    private static ValidationException Fail(string message) =>
        new(new Dictionary<string, string[]> { ["orderId"] = [message] });
}
