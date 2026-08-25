using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("customer_addresses");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.UserId).HasColumnName("user_id");
        builder.Property(a => a.Address).HasColumnName("address").IsRequired().HasMaxLength(255);
        builder.Property(a => a.City).HasColumnName("city").IsRequired().HasMaxLength(100);
        builder.Property(a => a.Neighborhood).HasColumnName("neighborhood").HasMaxLength(100);
        builder.Property(a => a.Reference).HasColumnName("reference").HasMaxLength(255);
        builder.Property(a => a.IsDefault).HasColumnName("is_default").IsRequired().HasDefaultValue(false);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()");

        builder.HasOne(a => a.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
