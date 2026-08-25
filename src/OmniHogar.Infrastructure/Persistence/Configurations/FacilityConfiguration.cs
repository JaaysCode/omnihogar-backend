using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> builder)
    {
        builder.ToTable("facilities", t => t.HasCheckConstraint("CK_facilities_type", "type IN ('WAREHOUSE','POS')"));

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(f => f.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        builder.Property(f => f.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(f => f.Address).HasColumnName("address").IsRequired().HasMaxLength(255);
        builder.Property(f => f.City).HasColumnName("city").IsRequired().HasMaxLength(100);
    }
}
