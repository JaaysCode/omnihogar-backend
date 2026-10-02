using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Constants;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Features.Orders;

/// <summary>
/// Opens an order's <see cref="Dispatch"/> record (if missing) and notifies the despacho team
/// in-app that it's ready to prepare (HU-13). Shared side effect, the same way
/// <see cref="Checkout.PaymentReconciler"/> is shared by the two payment-confirmation handlers —
/// invoked by <see cref="UpdateOrderStatusCommandHandler"/> whenever an order moves to
/// <c>preparing</c>.
///
/// Notification delivery is best-effort and isolated from the order's state change (crit. 3):
/// a failure here never throws back into the caller — it's logged, and <see cref="Dispatch.NotifiedAt"/>
/// stays null so the order is left pending of notification instead of silently losing it.
/// </summary>
public static class DispatchNotifier
{
    public static async Task<bool> NotifyAsync(
        IApplicationDbContext context,
        Order order,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        var dispatch = order.Dispatch;
        if (dispatch is null)
        {
            dispatch = new Dispatch
            {
                OrderId = order.Id,
                SourceFacilityId = order.FacilityId,
                Status = "preparing",
                StartedAt = DateTime.UtcNow,
            };
            context.Dispatches.Add(dispatch);
            order.Dispatch = dispatch;
        }

        if (dispatch.NotifiedAt is not null)
        {
            return true;
        }

        try
        {
            var recipientIds = await context.Users
                .Where(u => u.Status && u.UserRoles.Any(ur => ur.RoleId == SeededRoleIds.CoordinadorDeDespacho))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            if (recipientIds.Count == 0)
            {
                throw new InvalidOperationException("No hay usuarios activos en el rol Coordinador de Despacho.");
            }

            var content = $"Pedido {order.OrderNumber} listo para despacho.";
            foreach (var recipientId in recipientIds)
            {
                context.Notifications.Add(new Notification
                {
                    UserId = recipientId,
                    OrderId = order.Id,
                    Type = "dispatch_ready",
                    Channel = "in_app",
                    Content = content,
                    DeliveryStatus = "sent",
                });
            }

            dispatch.NotifiedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "No se pudo notificar el inicio de despacho del pedido {OrderId}.", order.Id);
            // Still persist the opened Dispatch row so the order stays flagged/pending of
            // notification rather than losing the transition.
            await context.SaveChangesAsync(cancellationToken);
            return false;
        }
    }
}
