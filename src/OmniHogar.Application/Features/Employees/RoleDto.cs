namespace OmniHogar.Application.Features.Employees;

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Names of the permissions this role grants (HU-31). Empty for a role with none.</summary>
    public List<string> Permissions { get; set; } = [];
}
