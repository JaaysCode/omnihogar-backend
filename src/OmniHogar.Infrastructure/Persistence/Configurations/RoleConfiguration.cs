using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Constants;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.Name).HasColumnName("name").IsRequired().HasMaxLength(50);
        builder.Property(r => r.Description).HasColumnName("description").HasMaxLength(255);

        builder.HasIndex(r => r.Name).IsUnique();

        builder.HasData(
            new Role
            {
                Id = SeededRoleIds.Administrador,
                Name = "Administrador",
                Description = "Acceso total al sistema.",
            },
            new Role
            {
                Id = SeededRoleIds.JefeDeBodega,
                Name = "Jefe de Bodega",
                Description = "Gestión de inventario y recepción de mercancía.",
            },
            new Role
            {
                Id = SeededRoleIds.AsesorDeTienda,
                Name = "Asesor de Tienda",
                Description = "Punto de venta y atención en tienda.",
            },
            new Role
            {
                Id = SeededRoleIds.CoordinadorDeDespacho,
                Name = "Coordinador de Despacho",
                Description = "Preparación y despacho de pedidos.",
            },
            new Role
            {
                Id = SeededRoleIds.Cliente,
                Name = "Cliente",
                Description = "Cuenta de cliente para compras y consulta de pedidos.",
            });
    }
}
