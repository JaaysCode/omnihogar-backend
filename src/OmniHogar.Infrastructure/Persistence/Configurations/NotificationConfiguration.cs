using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", t =>
        {
            t.HasCheckConstraint("CK_notifications_type", "type IN ('order_confirmed','payment_approved','in_dispatch','delivered','dispatch_ready')");
            t.HasCheckConstraint("CK_notifications_channel", "channel IN ('email','whatsapp','sms','in_app')");
            t.HasCheckConstraint("CK_notifications_delivery_status", "delivery_status IN ('pending','sent','failed')");
        });

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(n => n.UserId).HasColumnName("user_id");
        builder.Property(n => n.TemplateId).HasColumnName("template_id");
        builder.Property(n => n.Type).HasColumnName("type").IsRequired().HasMaxLength(30);
        builder.Property(n => n.Channel).HasColumnName("channel").IsRequired().HasMaxLength(20);
        builder.Property(n => n.Content).HasColumnName("content").IsRequired();
        builder.Property(n => n.DeliveryStatus).HasColumnName("delivery_status").IsRequired().HasMaxLength(20).HasDefaultValue("pending");
        builder.Property(n => n.OrderId).HasColumnName("order_id");
        builder.Property(n => n.IsRead).HasColumnName("is_read").IsRequired().HasDefaultValue(false);
        builder.Property(n => n.ReadAt).HasColumnName("read_at");
        builder.Property(n => n.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()");

        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.OrderId);

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Template)
            .WithMany(t => t.Notifications)
            .HasForeignKey(n => n.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Order)
            .WithMany()
            .HasForeignKey(n => n.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
