namespace OmniHogar.Domain.Entities;

/// <summary>Sellable catalog item (products).</summary>
public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid? CategoryId { get; set; }
    public ProductCategory? Category { get; set; }

    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>Allowed: active, discontinued.</summary>
    public string Status { get; set; } = "active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
