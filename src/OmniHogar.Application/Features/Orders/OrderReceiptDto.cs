namespace OmniHogar.Application.Features.Orders;

/// <summary>One product line printed on a payment receipt.</summary>
public record OrderReceiptItemDto(string ProductName, string Sku, int Quantity, decimal UnitPrice, decimal Subtotal);

/// <summary>
/// Payment receipt (comprobante) of an order whose payment was approved (HU-20): order number,
/// date, amount paid and payment method, plus the purchased products.
/// </summary>
public record OrderReceiptDto(
    string ReceiptNumber,
    Guid OrderId,
    string OrderNumber,
    DateTime PaidAt,
    decimal Amount,
    string PaymentMethod,
    string? Reference,
    string CustomerName,
    List<OrderReceiptItemDto> Items,
    decimal Subtotal,
    decimal Total);
