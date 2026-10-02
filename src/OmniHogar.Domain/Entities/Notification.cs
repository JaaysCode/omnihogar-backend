namespace OmniHogar.Domain.Entities;

/// <summary>Outbound notification sent to a user (notifications).</summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? TemplateId { get; set; }
    public NotificationTemplate? Template { get; set; }

    /// <summary>
    /// Allowed: order_confirmed, payment_approved, in_dispatch, delivered, dispatch_ready.
    /// <c>dispatch_ready</c> is the internal, in-app alert sent to the despacho team (HU-13) —
    /// distinct from <c>in_dispatch</c>, which is the customer-facing "your order shipped" message.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Allowed: email, whatsapp, sms, in_app. <c>in_app</c> is shown in the topbar bell only.</summary>
    public string Channel { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>Allowed: pending, sent, failed.</summary>
    public string DeliveryStatus { get; set; } = "pending";

    /// <summary>Order this notification refers to, so the UI can deep-link to it. Optional —
    /// not every notification type is order-bound.</summary>
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }

    /// <summary>Whether the recipient has opened/dismissed this notification in the topbar bell.</summary>
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
