namespace OmniHogar.Domain.Entities;

/// <summary>Audit trail of order status transitions (order_status_history).</summary>
public class OrderStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>Null when the change was automatic.</summary>
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
