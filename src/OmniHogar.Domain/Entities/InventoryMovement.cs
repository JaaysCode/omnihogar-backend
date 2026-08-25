namespace OmniHogar.Domain.Entities;

/// <summary>Individual stock change event (inventory_movements).</summary>
public class InventoryMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid FacilityId { get; set; }
    public Facility Facility { get; set; } = null!;

    /// <summary>Allowed: inbound, outbound, adjustment, transfer.</summary>
    public string Type { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public string? Reason { get; set; }

    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
