namespace OmniHogar.Domain.Entities;

/// <summary>Unified sales order record across channels (orders).</summary>
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrderNumber { get; set; } = string.Empty;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Allowed: web, store, chat.</summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>Physical facility where the sale was placed, for in-store sales.</summary>
    public Guid? FacilityId { get; set; }
    public Facility? Facility { get; set; }

    /// <summary>Advisor who registered the sale.</summary>
    public Guid? AdvisorId { get; set; }
    public User? Advisor { get; set; }

    public Guid? ShippingAddressId { get; set; }
    public CustomerAddress? ShippingAddress { get; set; }

    /// <summary>Allowed: pending_payment, payment_approved, preparing, packed, shipped, delivered, cancelled, payment_rejected.</summary>
    public string Status { get; set; } = "pending_payment";

    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public Dispatch? Dispatch { get; set; }
}
