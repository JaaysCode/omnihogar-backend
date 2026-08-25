using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class ConversationOrderConfiguration : IEntityTypeConfiguration<ConversationOrder>
{
    public void Configure(EntityTypeBuilder<ConversationOrder> builder)
    {
        builder.ToTable("conversation_orders");

        builder.HasKey(co => new { co.ConversationId, co.OrderId });

        builder.Property(co => co.ConversationId).HasColumnName("conversation_id");
        builder.Property(co => co.OrderId).HasColumnName("order_id");

        builder.HasOne(co => co.Conversation)
            .WithMany(c => c.ConversationOrders)
            .HasForeignKey(co => co.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(co => co.Order)
            .WithMany()
            .HasForeignKey(co => co.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
