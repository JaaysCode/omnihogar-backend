using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", t => t.HasCheckConstraint(
            "CK_users_user_type", "user_type IN ('customer','employee')"));

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.UserType).HasColumnName("user_type").IsRequired().HasMaxLength(20);
        builder.Property(u => u.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(100);
        builder.Property(u => u.Email).HasColumnName("email").IsRequired().HasMaxLength(150);
        builder.Property(u => u.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(255);
        builder.Property(u => u.LocationId).HasColumnName("location_id");
        builder.Property(u => u.Status).HasColumnName("status").IsRequired().HasDefaultValue(true);
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()");

        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasOne(u => u.Location)
            .WithMany()
            .HasForeignKey(u => u.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
