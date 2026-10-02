using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class DispatchConfiguration : IEntityTypeConfiguration<Dispatch>
{
    public void Configure(EntityTypeBuilder<Dispatch> builder)
    {
        builder.ToTable("dispatches", t => t.HasCheckConstraint(
            "CK_dispatches_status", "status IN ('pending','preparing','packed','shipped','delivered')"));

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.OrderId).HasColumnName("order_id");
        builder.Property(d => d.SourceFacilityId).HasColumnName("source_facility_id");
        builder.Property(d => d.HandlerId).HasColumnName("handler_id");
        builder.Property(d => d.Status).HasColumnName("status").IsRequired().HasMaxLength(20).HasDefaultValue("pending");
        builder.Property(d => d.StartedAt).HasColumnName("started_at");
        builder.Property(d => d.PackedAt).HasColumnName("packed_at");
        builder.Property(d => d.NotifiedAt).HasColumnName("notified_at");

        builder.HasIndex(d => d.OrderId).IsUnique();

        builder.HasOne(d => d.Order)
            .WithOne(o => o.Dispatch)
            .HasForeignKey<Dispatch>(d => d.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.SourceFacility)
            .WithMany()
            .HasForeignKey(d => d.SourceFacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Handler)
            .WithMany()
            .HasForeignKey(d => d.HandlerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
