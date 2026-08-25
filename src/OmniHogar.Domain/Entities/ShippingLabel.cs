namespace OmniHogar.Domain.Entities;

/// <summary>Carrier label issued for a dispatch (shipping_labels).</summary>
public class ShippingLabel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DispatchId { get; set; }
    public Dispatch Dispatch { get; set; } = null!;

    public string Carrier { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
