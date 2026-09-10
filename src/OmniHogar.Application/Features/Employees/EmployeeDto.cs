namespace OmniHogar.Application.Features.Employees;

/// <summary>Row shape for the admin "Gestión de Usuarios" list — employee accounts only.</summary>
public class EmployeeDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Null if the account somehow has no role assigned yet.</summary>
    public string? RoleName { get; set; }

    /// <summary>Id of the assigned role, for the "cambiar rol" editor (HU-31).</summary>
    public Guid? RoleId { get; set; }

    public bool Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
