namespace OmniHogar.Application.Features.Profile;

/// <summary>Own-account profile shape (HU-16) — same for every role (cliente/empleado); the
/// employee-only fields (rol, sede) live on <c>EmployeeDto</c>, not here.</summary>
public class ProfileDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
}
