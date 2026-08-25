using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("order_status_history");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(h => h.OrderId).HasColumnName("order_id");
        builder.Property(h => h.PreviousStatus).HasColumnName("previous_status").HasMaxLength(30);
        builder.Property(h => h.NewStatus).HasColumnName("new_status").IsRequired().HasMaxLength(30);
        builder.Property(h => h.UserId).HasColumnName("user_id");
        builder.Property(h => h.Comment).HasColumnName("comment").HasMaxLength(255);
        builder.Property(h => h.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()");

        builder.HasOne(h => h.Order)
            .WithMany(o => o.StatusHistory)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
