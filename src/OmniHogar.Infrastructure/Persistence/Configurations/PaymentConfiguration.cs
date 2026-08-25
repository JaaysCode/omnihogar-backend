using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", t =>
        {
            t.HasCheckConstraint("CK_payments_payment_method", "payment_method IN ('card','pse','wallet')");
            t.HasCheckConstraint("CK_payments_status", "status IN ('pending','approved','rejected','reversed')");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.OrderId).HasColumnName("order_id");
        builder.Property(p => p.PaymentMethod).HasColumnName("payment_method").IsRequired().HasMaxLength(30);
        builder.Property(p => p.Amount).HasColumnName("amount").HasPrecision(12, 2).IsRequired();
        builder.Property(p => p.Status).HasColumnName("status").IsRequired().HasMaxLength(20).HasDefaultValue("pending");
        builder.Property(p => p.GatewayTransactionId).HasColumnName("gateway_transaction_id").HasMaxLength(100);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()");
        builder.Property(p => p.ConfirmedAt).HasColumnName("confirmed_at");

        builder.HasIndex(p => p.OrderId);

        builder.HasOne(p => p.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
