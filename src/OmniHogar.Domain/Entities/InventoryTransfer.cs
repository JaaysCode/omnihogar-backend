namespace OmniHogar.Domain.Entities;

/// <summary>Stock movement requested between two facilities (inventory_transfers).</summary>
public class InventoryTransfer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid SourceFacilityId { get; set; }
    public Facility SourceFacility { get; set; } = null!;

    public Guid DestinationFacilityId { get; set; }
    public Facility DestinationFacility { get; set; } = null!;

    public int Quantity { get; set; }

    /// <summary>Allowed: pending, in_transit, completed.</summary>
    public string Status { get; set; } = "pending";

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
