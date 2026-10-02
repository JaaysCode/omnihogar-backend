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
    public DateTime CreatedAt { get; set; }
}
