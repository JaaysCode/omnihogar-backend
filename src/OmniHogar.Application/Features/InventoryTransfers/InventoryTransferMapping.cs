using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Features.InventoryTransfers;

internal static class InventoryTransferMapping
{
    /// <summary>Requires Product, SourceFacility and DestinationFacility to be loaded.</summary>
    public static InventoryTransferDto ToDto(this InventoryTransfer t) => new()
    {
        Id = t.Id,
        ProductId = t.ProductId,
        ProductName = t.Product.Name,
        Sku = t.Product.Sku,
        SourceFacilityId = t.SourceFacilityId,
        SourceFacilityName = t.SourceFacility.Name,
        DestinationFacilityId = t.DestinationFacilityId,
        DestinationFacilityName = t.DestinationFacility.Name,
        Quantity = t.Quantity,
        Status = t.Status,
        RequestedAt = t.RequestedAt,
        CompletedAt = t.CompletedAt,
    };

    public static ValidationException InsufficientStock(int requested, int available) =>
        new(new Dictionary<string, string[]>
        {
            ["quantity"] = [$"No hay unidades suficientes en el origen. Solicitadas: {requested}, disponibles: {available}."],
        });
}
