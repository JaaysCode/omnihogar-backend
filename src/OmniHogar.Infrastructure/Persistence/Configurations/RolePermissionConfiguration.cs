using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Constants;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.Property(rp => rp.RoleId).HasColumnName("role_id");
        builder.Property(rp => rp.PermissionId).HasColumnName("permission_id");

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(BuildSeed());
    }

    // Administrador → todos; el resto según su función. Cliente no tiene permisos (el catálogo
    // público es anónimo).
    private static IEnumerable<RolePermission> BuildSeed()
    {
        foreach (var permissionId in AllPermissionIds())
        {
            yield return new RolePermission { RoleId = SeededRoleIds.Administrador, PermissionId = permissionId };
        }

        foreach (var permissionId in new[]
                 {
                     SeededPermissionIds.InventarioConsultar,
                     SeededPermissionIds.InventarioAjustar,
                     SeededPermissionIds.ProductosVerCatalogo,
                     SeededPermissionIds.PedidosConsultar,
                 })
        {
            yield return new RolePermission { RoleId = SeededRoleIds.JefeDeBodega, PermissionId = permissionId };
        }

        foreach (var permissionId in new[]
                 {
                     SeededPermissionIds.InventarioConsultar,
                     SeededPermissionIds.PedidosConsultar,
                     SeededPermissionIds.PedidosActualizarEstado,
                 })
        {
            yield return new RolePermission { RoleId = SeededRoleIds.CoordinadorDeDespacho, PermissionId = permissionId };
        }

        foreach (var permissionId in new[]
                 {
                     SeededPermissionIds.InventarioConsultar,
                     SeededPermissionIds.PosRegistrarVenta,
                     SeededPermissionIds.PedidosConsultar,
                     SeededPermissionIds.ProductosVerCatalogo,
                 })
        {
            yield return new RolePermission { RoleId = SeededRoleIds.AsesorDeTienda, PermissionId = permissionId };
        }
    }

    private static Guid[] AllPermissionIds() =>
    [
        SeededPermissionIds.UsuariosGestionar,
        SeededPermissionIds.ProductosGestionar,
        SeededPermissionIds.ProductosVerCatalogo,
        SeededPermissionIds.InventarioConsultar,
        SeededPermissionIds.InventarioAjustar,
        SeededPermissionIds.PedidosConsultar,
        SeededPermissionIds.PedidosActualizarEstado,
        SeededPermissionIds.PosRegistrarVenta,
    ];
}
