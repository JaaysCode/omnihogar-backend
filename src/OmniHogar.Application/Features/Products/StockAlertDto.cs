namespace OmniHogar.Application.Features.Products;

/// <summary>
/// A low-stock alert (HU-24): a product at a facility whose available units are at or below the
/// configured minimum level.
/// </summary>
public record StockAlertDto(
    Guid ProductId,
    string Sku,
    string ProductName,
    Guid FacilityId,
    string FacilityName,
    int AvailableQuantity,
    int MinimumStock);
