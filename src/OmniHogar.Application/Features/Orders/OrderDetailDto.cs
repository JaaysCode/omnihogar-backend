namespace OmniHogar.Application.Features.Orders;

/// <summary>Line item within an order's detail view — product, quantity, and value.</summary>
public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>Full detail of a single order — products, quantities, value, client, channel, and status.</summary>
public class OrderDetailDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();

    /// <summary>
    /// HU-13 — set only on the response of the call that moved this order into <c>preparing</c>.
    /// True: despacho team notified. False: the order is "preparing" but notification failed and
    /// is pending retry. Null on every other request (status unchanged by this call).
    /// </summary>
    public bool? DispatchNotified { get; set; }
}
