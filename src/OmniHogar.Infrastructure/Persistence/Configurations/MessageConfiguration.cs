using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages", t => t.HasCheckConstraint(
            "CK_messages_sender", "sender IN ('customer','bot','advisor')"));

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(m => m.ConversationId).HasColumnName("conversation_id");
        builder.Property(m => m.Sender).HasColumnName("sender").IsRequired().HasMaxLength(20);
        builder.Property(m => m.Content).HasColumnName("content").IsRequired();
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()");

        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
