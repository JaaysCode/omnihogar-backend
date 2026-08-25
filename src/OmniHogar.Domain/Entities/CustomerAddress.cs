namespace OmniHogar.Domain.Entities;

/// <summary>Saved delivery address for a customer (customer_addresses).</summary>
public class CustomerAddress
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Neighborhood { get; set; }
    public string? Reference { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
