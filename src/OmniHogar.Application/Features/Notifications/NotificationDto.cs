namespace OmniHogar.Application.Features.Notifications;

/// <summary>Row shape for the topbar notification bell.</summary>
public class NotificationDto
{
    public Guid Id { get; set; }

    /// <summary>e.g. dispatch_ready.</summary>
    public string Type { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public bool IsRead { get; set; }

    /// <summary>pending | sent | failed — a failed row records an order notification that could
    /// not be delivered (HU-26 crit. 3).</summary>
    public string DeliveryStatus { get; set; } = "sent";
    public DateTime CreatedAt { get; set; }
}
