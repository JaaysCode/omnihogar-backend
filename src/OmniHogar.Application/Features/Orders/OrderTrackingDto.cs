namespace OmniHogar.Application.Features.Orders;

/// <summary>One step in an order's status trail, as shown to the customer.</summary>
public class OrderTrackingEventDto
{
    public string Status { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}

/// <summary>
/// Customer-facing tracking view of an order (HU-14 seguimiento): current status, when it last
/// changed, and the trail of status changes oldest first. Staff comments and user ids on the
/// audit rows are deliberately left out.
/// </summary>
public class OrderTrackingDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public List<OrderTrackingEventDto> History { get; set; } = new();
}
