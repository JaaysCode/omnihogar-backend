using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations", t =>
        {
            t.HasCheckConstraint("CK_conversations_channel", "channel IN ('whatsapp','messenger','instagram','web')");
            t.HasCheckConstraint("CK_conversations_status", "status IN ('open','escalated','closed')");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.CustomerId).HasColumnName("customer_id");
        builder.Property(c => c.Channel).HasColumnName("channel").IsRequired().HasMaxLength(20);
        builder.Property(c => c.Status).HasColumnName("status").IsRequired().HasMaxLength(20).HasDefaultValue("open");
        builder.Property(c => c.StartedAt).HasColumnName("started_at").IsRequired().HasDefaultValueSql("now()");

        builder.HasOne(c => c.Customer)
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
