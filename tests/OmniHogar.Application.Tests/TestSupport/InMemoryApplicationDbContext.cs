using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Tests.TestSupport;

/// <summary>
/// Minimal EF Core InMemory-backed implementation of <see cref="IApplicationDbContext"/> used
/// to exercise Application handlers/validators without depending on the Infrastructure project
/// or a real database.
/// </summary>
public class InMemoryApplicationDbContext : DbContext, IApplicationDbContext
{
    public InMemoryApplicationDbContext(DbContextOptions<InMemoryApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    // Bare-bones model: only what the handlers/validators under test touch. Navigation
    // properties pointing at entities outside this fake context's DbSets are ignored
    // rather than fully mapped (this context intentionally doesn't model the whole schema).
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(builder =>
        {
            builder.HasKey(u => u.Id);
            builder.Ignore(u => u.UserRoles);
            builder.Ignore(u => u.Addresses);
            builder.Ignore(u => u.Orders);
            builder.Ignore(u => u.Facility);
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Ignore(p => p.Category);
        });

        modelBuilder.Entity<Role>(builder =>
        {
            builder.HasKey(r => r.Id);
            builder.Ignore(r => r.UserRoles);
            builder.Ignore(r => r.RolePermissions);
        });

        modelBuilder.Entity<UserRole>(builder =>
        {
            builder.HasKey(ur => new { ur.UserId, ur.RoleId });
        });

        modelBuilder.Entity<ProductCategory>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Ignore(c => c.Products);
        });
    }

    public static InMemoryApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<InMemoryApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InMemoryApplicationDbContext(options);
    }
}
