using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("inventory_movements", t => t.HasCheckConstraint(
            "CK_inventory_movements_type", "type IN ('inbound','outbound','adjustment','transfer')"));

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(m => m.ProductId).HasColumnName("product_id");
        builder.Property(m => m.FacilityId).HasColumnName("facility_id");
        builder.Property(m => m.Type).HasColumnName("type").IsRequired().HasMaxLength(20);
        builder.Property(m => m.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(m => m.Reason).HasColumnName("reason").HasMaxLength(255);
        builder.Property(m => m.OrderId).HasColumnName("order_id");
        builder.Property(m => m.UserId).HasColumnName("user_id");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()");

        builder.HasIndex(m => m.ProductId);

        builder.HasOne(m => m.Product)
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Facility)
            .WithMany()
            .HasForeignKey(m => m.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Order)
            .WithMany()
            .HasForeignKey(m => m.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
