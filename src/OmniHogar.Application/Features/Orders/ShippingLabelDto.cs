using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Features.Orders;

/// <summary>One product line printed on the shipping guide.</summary>
public record ShippingLabelItemDto(string ProductName, string Sku, int Quantity);

/// <summary>
/// Shipping guide for an order (HU-25): order id, delivery address and the data a carrier needs.
/// </summary>
public record ShippingLabelDto(
    Guid OrderId,
    string OrderNumber,
    string Carrier,
    string TrackingNumber,
    DateTime GeneratedAt,
    string CustomerName,
    string? CustomerPhone,
    string Address,
    string City,
    string? Neighborhood,
    string? Reference,
    List<ShippingLabelItemDto> Items)
{
    /// <summary>Expects <paramref name="order"/> loaded with User, ShippingAddress and Items.Product.</summary>
    public static ShippingLabelDto From(Order order, ShippingLabel label)
    {
        var address = order.ShippingAddress!;
        return new ShippingLabelDto(
            order.Id,
            order.OrderNumber,
            label.Carrier,
            label.TrackingNumber,
            label.GeneratedAt,
            $"{order.User.FirstName} {order.User.LastName}",
            order.User.Phone,
            address.Address,
            address.City,
            address.Neighborhood,
            address.Reference,
            order.Items
                .Select(i => new ShippingLabelItemDto(i.Product.Name, i.Product.Sku, i.Quantity))
                .ToList());
    }
}
