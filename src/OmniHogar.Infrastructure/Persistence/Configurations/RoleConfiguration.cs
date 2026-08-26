using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
                Id = new Guid("11111111-1111-1111-1111-111111111111"),
                Name = "Admin",
                Description = "Full system access.",
            },
            new Role
            {
                Id = new Guid("22222222-2222-2222-2222-222222222222"),
                Name = "Manager",
                Description = "Store and inventory management.",
            },
            new Role
            {
                Id = new Guid("33333333-3333-3333-3333-333333333333"),
                Name = "Sales",
                Description = "Point of sale and order handling.",
            });
    }
}
