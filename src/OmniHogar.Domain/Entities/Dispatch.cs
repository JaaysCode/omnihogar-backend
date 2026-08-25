namespace OmniHogar.Domain.Entities;

/// <summary>Fulfillment/shipping process for an order (dispatches).</summary>
public class Dispatch
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid? SourceFacilityId { get; set; }
    public Facility? SourceFacility { get; set; }

    public Guid? HandlerId { get; set; }
    public User? Handler { get; set; }

    /// <summary>Allowed: pending, preparing, packed, shipped, delivered.</summary>
    public string Status { get; set; } = "pending";

    public DateTime? StartedAt { get; set; }
    public DateTime? PackedAt { get; set; }

    public ICollection<ShippingLabel> ShippingLabels { get; set; } = new List<ShippingLabel>();
}
