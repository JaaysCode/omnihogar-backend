using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", t => t.HasCheckConstraint("CK_order_items_quantity", "quantity > 0"));

        builder.HasKey(oi => oi.Id);
        builder.Property(oi => oi.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(oi => oi.OrderId).HasColumnName("order_id");
        builder.Property(oi => oi.ProductId).HasColumnName("product_id");
        builder.Property(oi => oi.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(oi => oi.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2).IsRequired();
        builder.Property(oi => oi.Subtotal).HasColumnName("subtotal").HasPrecision(12, 2).IsRequired();

        builder.HasIndex(oi => oi.OrderId);

        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
