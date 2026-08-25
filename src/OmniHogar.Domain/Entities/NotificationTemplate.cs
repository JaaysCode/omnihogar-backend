namespace OmniHogar.Domain.Entities;

/// <summary>Reusable message template for a notification channel (notification_templates).</summary>
public class NotificationTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;

    /// <summary>Allowed: email, whatsapp, sms.</summary>
    public string Channel { get; set; } = string.Empty;

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
