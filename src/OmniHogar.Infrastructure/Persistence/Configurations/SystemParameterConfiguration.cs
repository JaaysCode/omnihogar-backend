using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class SystemParameterConfiguration : IEntityTypeConfiguration<SystemParameter>
{
    public void Configure(EntityTypeBuilder<SystemParameter> builder)
    {
        builder.ToTable("system_parameters");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.Key).HasColumnName("key").IsRequired().HasMaxLength(100);
        builder.Property(s => s.Value).HasColumnName("value").IsRequired().HasMaxLength(255);
        builder.Property(s => s.Description).HasColumnName("description").HasMaxLength(255);

        builder.HasIndex(s => s.Key).IsUnique();
    }
}
