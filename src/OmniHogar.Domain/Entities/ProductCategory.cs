namespace OmniHogar.Domain.Entities;

/// <summary>Product classification (product_categories).</summary>
public class ProductCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
