namespace OmniHogar.Domain.Entities;

/// <summary>Outbound notification sent to a user (notifications).</summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? TemplateId { get; set; }
    public NotificationTemplate? Template { get; set; }

    /// <summary>Allowed: order_confirmed, payment_approved, in_dispatch, delivered.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Allowed: email, whatsapp, sms.</summary>
    public string Channel { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>Allowed: pending, sent, failed.</summary>
    public string DeliveryStatus { get; set; } = "pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
