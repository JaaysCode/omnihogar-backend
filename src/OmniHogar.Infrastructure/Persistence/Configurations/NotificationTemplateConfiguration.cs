using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates", t => t.HasCheckConstraint(
            "CK_notification_templates_channel", "channel IN ('email','whatsapp','sms')"));

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(n => n.Code).HasColumnName("code").IsRequired().HasMaxLength(50);
        builder.Property(n => n.Subject).HasColumnName("subject").HasMaxLength(150);
        builder.Property(n => n.Body).HasColumnName("body").IsRequired();
        builder.Property(n => n.Channel).HasColumnName("channel").IsRequired().HasMaxLength(20);

        builder.HasIndex(n => n.Code).IsUnique();
    }
}
