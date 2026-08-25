namespace OmniHogar.Domain.Entities;

/// <summary>Payment attempt against an order (payments).</summary>
public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    /// <summary>Allowed: card, pse, wallet.</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    /// <summary>Allowed: pending, approved, rejected, reversed.</summary>
    public string Status { get; set; } = "pending";

    public string? GatewayTransactionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
}
