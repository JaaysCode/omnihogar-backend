namespace OmniHogar.Application.Features.Orders;

/// <summary>Response of a successful in-store sale (HU-06) — a receipt with the new order's id.</summary>
public class StoreSaleResultDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Sum of every line's price × quantity, before tax.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>19% IVA over <see cref="Subtotal"/>.</summary>
    public decimal Tax { get; set; }

    /// <summary><see cref="Subtotal"/> + <see cref="Tax"/> — the amount charged.</summary>
    public decimal Total { get; set; }

    public DateTime CreatedAt { get; set; }
    public List<StoreSaleItemResultDto> Items { get; set; } = [];
}

public class StoreSaleItemResultDto
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}
