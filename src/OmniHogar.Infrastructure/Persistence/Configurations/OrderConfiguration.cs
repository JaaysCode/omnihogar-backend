using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", t =>
        {
            t.HasCheckConstraint("CK_orders_channel", "channel IN ('web','store','chat')");
            t.HasCheckConstraint("CK_orders_status", "status IN ('pending_payment','payment_approved','preparing','packed','shipped','delivered','cancelled','payment_rejected')");
        });

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(o => o.OrderNumber).HasColumnName("order_number").IsRequired().HasMaxLength(30);
        builder.Property(o => o.UserId).HasColumnName("user_id");
        builder.Property(o => o.Channel).HasColumnName("channel").IsRequired().HasMaxLength(20);
        builder.Property(o => o.FacilityId).HasColumnName("facility_id");
        builder.Property(o => o.AdvisorId).HasColumnName("advisor_id");
        builder.Property(o => o.ShippingAddressId).HasColumnName("shipping_address_id");
        builder.Property(o => o.Status).HasColumnName("status").IsRequired().HasMaxLength(30).HasDefaultValue("pending_payment");
        builder.Property(o => o.Subtotal).HasColumnName("subtotal").HasPrecision(12, 2).IsRequired();
        builder.Property(o => o.Total).HasColumnName("total").HasPrecision(12, 2).IsRequired();
        builder.Property(o => o.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()");

        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.Status);

        builder.HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Facility)
            .WithMany()
            .HasForeignKey(o => o.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Advisor)
            .WithMany()
            .HasForeignKey(o => o.AdvisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.ShippingAddress)
            .WithMany()
            .HasForeignKey(o => o.ShippingAddressId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
