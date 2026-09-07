using System.Text.Json.Serialization;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Features.Products;

public class ProductStockDto
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Sum of <see cref="FacilityStockDto.AvailableQuantity"/> across every facility.</summary>
    public int TotalAvailable { get; set; }

    /// <summary>False when <see cref="TotalAvailable"/> is 0 — no inventory rows, or every row is at 0.</summary>
    public bool InStock { get; set; }

    /// <summary>Per-facility breakdown (warehouses and points of sale). Empty when the product
    /// has no inventory record anywhere yet.</summary>
    public List<FacilityStockDto> Facilities { get; set; } = new();
}

public class FacilityStockDto
{
    public Guid FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;

    /// <summary>Serialized as its enum member name ("WAREHOUSE" or "POS") — see <see cref="FacilityType"/>.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FacilityType FacilityType { get; set; }
    public string City { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }
}
