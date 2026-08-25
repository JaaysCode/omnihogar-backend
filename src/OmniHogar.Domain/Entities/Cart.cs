namespace OmniHogar.Domain.Entities;

/// <summary>In-progress shopping cart (carts).</summary>
public class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Allowed: web, store, chat.</summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>Allowed: active, converted, abandoned.</summary>
    public string Status { get; set; } = "active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
