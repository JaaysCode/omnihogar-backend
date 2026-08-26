namespace OmniHogar.Domain.Entities;

/// <summary>Customer or employee account (users).</summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Allowed: customer, employee.</summary>
    public string UserType { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Assigned facility, for employees.</summary>
    public Guid? FacilityId { get; set; }
    public Facility? Facility { get; set; }

    public bool Status { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
