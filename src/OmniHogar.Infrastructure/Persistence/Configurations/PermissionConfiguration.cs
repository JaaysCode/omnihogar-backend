using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Constants;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(255);

        builder.HasIndex(p => p.Name).IsUnique();

        builder.HasData(
            new Permission { Id = SeededPermissionIds.UsuariosGestionar, Name = AppPermissions.UsuariosGestionar, Description = "Crear y consultar cuentas de empleados." },
            new Permission { Id = SeededPermissionIds.ProductosGestionar, Name = AppPermissions.ProductosGestionar, Description = "Crear, editar y eliminar productos." },
            new Permission { Id = SeededPermissionIds.ProductosVerCatalogo, Name = AppPermissions.ProductosVerCatalogo, Description = "Consultar el catálogo administrativo de productos." },
            new Permission { Id = SeededPermissionIds.InventarioConsultar, Name = AppPermissions.InventarioConsultar, Description = "Consultar unidades disponibles de un producto." },
            new Permission { Id = SeededPermissionIds.InventarioAjustar, Name = AppPermissions.InventarioAjustar, Description = "Agregar o ajustar unidades de inventario." },
            new Permission { Id = SeededPermissionIds.PedidosConsultar, Name = AppPermissions.PedidosConsultar, Description = "Consultar pedidos." },
            new Permission { Id = SeededPermissionIds.PosRegistrarVenta, Name = AppPermissions.PosRegistrarVenta, Description = "Registrar ventas en el punto de venta." });
    }
}
