namespace OmniHogar.Application.Features.Products;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = "active";

    /// <summary>
    /// Units available across all facilities (HU-05). Populated by the public catalog query
    /// (<see cref="GetProductsQuery"/>); <c>null</c> from queries that don't compute it.
    /// </summary>
    public int? AvailableQuantity { get; set; }

    /// <summary><c>true</c> when <see cref="AvailableQuantity"/> &gt; 0 (HU-05); <c>null</c> when not computed.</summary>
    public bool? InStock { get; set; }
}
