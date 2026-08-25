using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class ShippingLabelConfiguration : IEntityTypeConfiguration<ShippingLabel>
{
    public void Configure(EntityTypeBuilder<ShippingLabel> builder)
    {
        builder.ToTable("shipping_labels");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.DispatchId).HasColumnName("dispatch_id");
        builder.Property(s => s.Carrier).HasColumnName("carrier").IsRequired().HasMaxLength(50);
        builder.Property(s => s.TrackingNumber).HasColumnName("tracking_number").IsRequired().HasMaxLength(60);
        builder.Property(s => s.GeneratedAt).HasColumnName("generated_at").IsRequired().HasDefaultValueSql("now()");

        builder.HasOne(s => s.Dispatch)
            .WithMany(d => d.ShippingLabels)
            .HasForeignKey(s => s.DispatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
