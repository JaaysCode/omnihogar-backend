using OmniHogar.Domain.Common;

namespace OmniHogar.Domain.Entities;

/// <summary>
/// Sample domain entity for OmniHogar catalog. Replace/extend with real domain model.
/// </summary>
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
