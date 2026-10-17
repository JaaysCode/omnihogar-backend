namespace OmniHogar.Application.Features.InventoryTransfers;

/// <summary>Row shape for an inventory transfer between two facilities.</summary>
public class InventoryTransferDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public Guid SourceFacilityId { get; set; }
    public string SourceFacilityName { get; set; } = string.Empty;
    public Guid DestinationFacilityId { get; set; }
    public string DestinationFacilityName { get; set; } = string.Empty;
    public int Quantity { get; set; }

    /// <summary>pending (registered, stock untouched), in_transit (confirmed, deducted from the
    /// origin), or completed.</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>A physical location (warehouse or point of sale) that can be a transfer origin/destination.</summary>
public class FacilityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>"WAREHOUSE" or "POS".</summary>
    public string Type { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;
}
