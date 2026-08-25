using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", t => t.HasCheckConstraint(
            "CK_products_status", "status IN ('active','discontinued')"));

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Sku).HasColumnName("sku").IsRequired().HasMaxLength(20);
        builder.Property(p => p.Name).HasColumnName("name").IsRequired().HasMaxLength(150);
        builder.Property(p => p.Description).HasColumnName("description");
        builder.Property(p => p.CategoryId).HasColumnName("category_id");
        builder.Property(p => p.Price).HasColumnName("price").HasPrecision(12, 2).IsRequired();
        builder.Property(p => p.ImageUrl).HasColumnName("image_url").HasMaxLength(255);
        builder.Property(p => p.Status).HasColumnName("status").IsRequired().HasMaxLength(20).HasDefaultValue("active");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()");

        builder.HasIndex(p => p.Sku).IsUnique();

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
