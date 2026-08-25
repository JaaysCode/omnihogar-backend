namespace OmniHogar.Domain.Entities;

/// <summary>Granular action grantable to a role (permissions).</summary>
public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
