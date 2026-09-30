namespace OmniHogar.Application.Features.Cart;

/// <summary>The client's active shopping cart (HU-05). Empty <see cref="Items"/> = nothing added yet.</summary>
public class CartDto
{
    public Guid Id { get; set; }
    public List<CartItemDto> Items { get; set; } = [];

    /// <summary>Sum of every line subtotal.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>Amount payable. Product prices already include IVA and there's no shipping fee,
    /// so this equals <see cref="Subtotal"/> — nothing gets added later at checkout.</summary>
    public decimal Total { get; set; }

    /// <summary>Sum of every line's quantity.</summary>
    public int ItemCount { get; set; }
}

public class CartItemDto
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    /// <summary>Price snapshotted when the product was first added to the cart.</summary>
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    /// <summary><see cref="UnitPrice"/> × <see cref="Quantity"/>.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>Units currently available across all facilities — lets the UI cap the stepper.</summary>
    public int AvailableQuantity { get; set; }
}
