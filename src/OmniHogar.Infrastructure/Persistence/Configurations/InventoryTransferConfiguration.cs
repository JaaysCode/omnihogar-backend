using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class InventoryTransferConfiguration : IEntityTypeConfiguration<InventoryTransfer>
{
    public void Configure(EntityTypeBuilder<InventoryTransfer> builder)
    {
        builder.ToTable("inventory_transfers", t =>
        {
            t.HasCheckConstraint("CK_inventory_transfers_status", "status IN ('pending','in_transit','completed')");
            t.HasCheckConstraint("CK_inventory_transfers_quantity", "quantity > 0");
            t.HasCheckConstraint("CK_inventory_transfers_facilities", "source_facility_id <> destination_facility_id");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.SourceFacilityId).HasColumnName("source_facility_id");
        builder.Property(x => x.DestinationFacilityId).HasColumnName("destination_facility_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(20).HasDefaultValue("pending");
        builder.Property(x => x.RequestedAt).HasColumnName("requested_at").IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SourceFacility)
            .WithMany()
            .HasForeignKey(x => x.SourceFacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DestinationFacility)
            .WithMany()
            .HasForeignKey(x => x.DestinationFacilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
