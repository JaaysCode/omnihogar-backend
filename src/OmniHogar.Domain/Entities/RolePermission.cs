namespace OmniHogar.Domain.Entities;

/// <summary>Permission granted to a role (role_permissions, composite key).</summary>
public class RolePermission
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
