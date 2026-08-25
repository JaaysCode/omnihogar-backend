namespace OmniHogar.Domain.Entities;

/// <summary>Stock level of a product at a facility (inventory).</summary>
public class Inventory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid FacilityId { get; set; }
    public Facility Facility { get; set; } = null!;

    public int AvailableQuantity { get; set; }
    public int MinimumStock { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
