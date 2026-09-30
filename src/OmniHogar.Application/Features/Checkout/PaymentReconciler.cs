using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Features.Checkout;

/// <summary>
/// Applies a payment gateway status to our <see cref="Order"/>/<see cref="Payment"/> pair.
/// Shared by <see cref="GetCheckoutStatusQueryHandler"/> (redirect-back confirmation) and
/// <see cref="HandlePaymentGatewayWebhookCommandHandler"/> (webhook) — same outcome either way.
/// Idempotent: a <see cref="Payment"/> that's no longer "pending" is left untouched, so a
/// duplicate webhook delivery or a second status check never double-applies the side effects
/// (inventory deduction, cart conversion).
/// </summary>
public static class PaymentReconciler
{
    public static async Task ApplyAsync(
        IApplicationDbContext context,
        Order order,
        Payment payment,
        string gatewayStatus,
        CancellationToken cancellationToken)
    {
        if (payment.Status != "pending")
        {
            return;
        }

        switch (gatewayStatus)
        {
            case "approved":
                payment.Status = "approved";
                payment.ConfirmedAt = DateTime.UtcNow;
                order.Status = "payment_approved";
                await DeductInventoryAsync(context, order, cancellationToken);
                await ConvertCartAsync(context, order.UserId, cancellationToken);
                break;

            case "rejected":
            case "cancelled":
                payment.Status = "rejected";
                order.Status = "payment_rejected";
                break;

            default:
                // pending, in_process, authorized, ... — no change yet, still awaiting a final answer.
                break;
        }
    }

    private static async Task DeductInventoryAsync(IApplicationDbContext context, Order order, CancellationToken cancellationToken)
    {
        if (order.FacilityId is not Guid facilityId)
        {
            return;
        }

        var items = await context.OrderItems.Where(oi => oi.OrderId == order.Id).ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            var inventory = await context.Inventory
                .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.FacilityId == facilityId, cancellationToken);

            if (inventory is not null)
            {
                inventory.AvailableQuantity -= item.Quantity;
                inventory.UpdatedAt = DateTime.UtcNow;
            }

            context.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = item.ProductId,
                FacilityId = facilityId,
                Type = "outbound",
                Quantity = item.Quantity,
                Reason = "Venta en línea",
                OrderId = order.Id,
                UserId = order.UserId,
            });
        }
    }

    private static async Task ConvertCartAsync(IApplicationDbContext context, Guid userId, CancellationToken cancellationToken)
    {
        var cart = await context.Carts.FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "active", cancellationToken);
        if (cart is not null)
        {
            cart.Status = "converted";
        }
    }
}
