namespace OmniHogar.Application.Features.Orders;

/// <summary>Response of a successful in-store sale (HU-06) — a receipt with the new order's id.</summary>
public class StoreSaleResultDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Sum of every line's price × quantity. Prices are already IVA-inclusive
    /// (the sticker price the customer saw), so this equals <see cref="Total"/> — nothing
    /// added at the register.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>19% IVA backed OUT of <see cref="Subtotal"/> (not added to it) — for the
    /// receipt/bookkeeping breakdown of what's owed to DIAN, since the price already includes it.</summary>
    public decimal Tax { get; set; }

    /// <summary>The amount charged — equal to <see cref="Subtotal"/>, since tax is already
    /// embedded in each product's price.</summary>
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
