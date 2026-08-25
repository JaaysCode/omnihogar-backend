using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("cart_items", t => t.HasCheckConstraint("CK_cart_items_quantity", "quantity > 0"));

        builder.HasKey(ci => ci.Id);
        builder.Property(ci => ci.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(ci => ci.CartId).HasColumnName("cart_id");
        builder.Property(ci => ci.ProductId).HasColumnName("product_id");
        builder.Property(ci => ci.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(ci => ci.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2).IsRequired();

        builder.HasOne(ci => ci.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ci => ci.Product)
            .WithMany()
            .HasForeignKey(ci => ci.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
