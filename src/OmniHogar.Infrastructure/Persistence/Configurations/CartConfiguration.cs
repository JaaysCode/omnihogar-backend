using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Persistence.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("carts", t =>
        {
            t.HasCheckConstraint("CK_carts_channel", "channel IN ('web','store','chat')");
            t.HasCheckConstraint("CK_carts_status", "status IN ('active','converted','abandoned')");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.UserId).HasColumnName("user_id");
        builder.Property(c => c.Channel).HasColumnName("channel").IsRequired().HasMaxLength(20);
        builder.Property(c => c.Status).HasColumnName("status").IsRequired().HasMaxLength(20).HasDefaultValue("active");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()");

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
