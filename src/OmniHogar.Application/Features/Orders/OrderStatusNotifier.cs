using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Constants;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Features.Orders;

/// <summary>
/// Notifies the internal areas in-app when an order's status changes (HU-26). Entering
/// <c>preparing</c> is handled by <see cref="DispatchNotifier"/> (HU-13), which also opens the
/// Dispatch record; every other relevant status goes through here.
///
/// Delivery is best-effort and isolated from the order's state change: a failure never throws back
/// into the caller. Instead it is logged and recorded as a <c>failed</c> notification owned by the
/// user who made the change (crit. 3), so the problem stays visible for later follow-up.
/// </summary>
public static class OrderStatusNotifier
{
    public const string NotificationType = "order_status_changed";

    // Internal areas interested in each status. Administrador always receives every one of them.
    private static readonly Dictionary<string, Guid[]> RecipientRoles = new()
    {
        ["payment_approved"] = [SeededRoleIds.JefeDeBodega, SeededRoleIds.CoordinadorDeDespacho],
        ["packed"] = [SeededRoleIds.CoordinadorDeDespacho],
        ["shipped"] = [SeededRoleIds.AsesorDeTienda, SeededRoleIds.JefeDeBodega],
        ["delivered"] = [SeededRoleIds.AsesorDeTienda],
        ["cancelled"] = [SeededRoleIds.JefeDeBodega, SeededRoleIds.CoordinadorDeDespacho, SeededRoleIds.AsesorDeTienda],
        ["payment_rejected"] = [SeededRoleIds.AsesorDeTienda],
    };

    private static readonly Dictionary<string, string> StatusLabels = new()
    {
        ["pending_payment"] = "Pago pendiente",
        ["payment_approved"] = "Pago aprobado",
        ["preparing"] = "Preparando",
        ["packed"] = "Empacado",
        ["shipped"] = "Enviado",
        ["delivered"] = "Entregado",
        ["cancelled"] = "Cancelado",
        ["payment_rejected"] = "Pago rechazado",
    };

    /// <summary>Returns true when the notification was delivered (or the status needs none).</summary>
    public static async Task<bool> NotifyAsync(
        IApplicationDbContext context,
        Order order,
        string previousStatus,
        string newStatus,
        Guid? actorUserId,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        if (!RecipientRoles.TryGetValue(newStatus, out var areaRoles))
        {
            return true;
        }

        var added = new List<Notification>();
        try
        {
            var roleIds = areaRoles.Append(SeededRoleIds.Administrador).ToArray();
            var recipientIds = await context.Users
                .Where(u => u.Status && u.UserRoles.Any(ur => roleIds.Contains(ur.RoleId)))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            if (recipientIds.Count == 0)
            {
                throw new InvalidOperationException("No hay usuarios activos en las áreas que deben ser notificadas.");
            }

            var content = $"Pedido {order.OrderNumber}: {Label(previousStatus)} → {Label(newStatus)}.";
            foreach (var recipientId in recipientIds)
            {
                added.Add(new Notification
                {
                    UserId = recipientId,
                    OrderId = order.Id,
                    Type = NotificationType,
                    Channel = "in_app",
                    Content = content,
                    DeliveryStatus = "sent",
                });
            }

            context.Notifications.AddRange(added);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "No se pudo notificar el cambio del pedido {OrderId} a {NewStatus}.", order.Id, newStatus);
            // Drop whatever this attempt left pending so recording the failure can't re-trip on it.
            context.Notifications.RemoveRange(added);
            await RecordFailureAsync(context, order, actorUserId, ex.Message, logger, cancellationToken);
            return false;
        }
    }

    /// <summary>
    /// Persists a <c>failed</c> in-app notification for <paramref name="actorUserId"/> describing
    /// why an order notification could not be delivered. Never throws. Without an actor (system
    /// initiated change) the failure is only logged.
    /// </summary>
    public static async Task RecordFailureAsync(
        IApplicationDbContext context,
        Order order,
        Guid? actorUserId,
        string reason,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        if (actorUserId is null)
        {
            return;
        }

        try
        {
            context.Notifications.Add(new Notification
            {
                UserId = actorUserId.Value,
                OrderId = order.Id,
                Type = NotificationType,
                Channel = "in_app",
                Content = $"No se pudo notificar el cambio del pedido {order.OrderNumber}: {reason}",
                DeliveryStatus = "failed",
            });
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "No se pudo registrar el fallo de notificación del pedido {OrderId}.", order.Id);
        }
    }

    private static string Label(string status) => StatusLabels.GetValueOrDefault(status, status);
}
