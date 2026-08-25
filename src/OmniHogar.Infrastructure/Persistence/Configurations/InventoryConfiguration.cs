using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("inventory", t => t.HasCheckConstraint(
            "CK_inventory_available_quantity", "available_quantity >= 0"));

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(i => i.ProductId).HasColumnName("product_id");
        builder.Property(i => i.FacilityId).HasColumnName("facility_id");
        builder.Property(i => i.AvailableQuantity).HasColumnName("available_quantity").IsRequired().HasDefaultValue(0);
        builder.Property(i => i.MinimumStock).HasColumnName("minimum_stock").IsRequired().HasDefaultValue(0);
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()");

        builder.HasIndex(i => new { i.ProductId, i.FacilityId }).IsUnique();
        builder.HasIndex(i => i.ProductId);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Facility)
            .WithMany()
            .HasForeignKey(i => i.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
