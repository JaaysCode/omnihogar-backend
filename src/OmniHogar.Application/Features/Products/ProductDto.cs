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
}
