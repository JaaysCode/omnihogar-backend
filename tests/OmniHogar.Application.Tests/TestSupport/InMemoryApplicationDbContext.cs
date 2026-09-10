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
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<Inventory> Inventory => Set<Inventory>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Bare-bones model: only what the handlers/validators under test touch. Navigation
    // properties pointing at entities outside this fake context's DbSets are ignored
    // rather than fully mapped (this context intentionally doesn't model the whole schema).
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(builder =>
        {
            builder.HasKey(u => u.Id);
            builder.HasMany(u => u.UserRoles).WithOne(ur => ur.User).HasForeignKey(ur => ur.UserId);
            builder.Ignore(u => u.Addresses);
            builder.Ignore(u => u.Orders);
            builder.Ignore(u => u.Facility);
            builder.Ignore(u => u.RefreshTokens);
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Ignore(p => p.Category);
        });

        modelBuilder.Entity<Role>(builder =>
        {
            builder.HasKey(r => r.Id);
            builder.HasMany(r => r.UserRoles).WithOne(ur => ur.Role).HasForeignKey(ur => ur.RoleId);
            builder.HasMany(r => r.RolePermissions).WithOne(rp => rp.Role).HasForeignKey(rp => rp.RoleId);
        });

        modelBuilder.Entity<Permission>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.HasMany(p => p.RolePermissions).WithOne(rp => rp.Permission).HasForeignKey(rp => rp.PermissionId);
        });

        modelBuilder.Entity<RolePermission>(builder =>
        {
            builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });
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

        modelBuilder.Entity<Facility>(builder =>
        {
            builder.HasKey(f => f.Id);
        });

        modelBuilder.Entity<Inventory>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId);
            builder.HasOne(i => i.Facility).WithMany().HasForeignKey(i => i.FacilityId);
        });

        modelBuilder.Entity<InventoryMovement>(builder =>
        {
            builder.HasKey(m => m.Id);
            builder.HasOne(m => m.Product).WithMany().HasForeignKey(m => m.ProductId);
            builder.HasOne(m => m.Facility).WithMany().HasForeignKey(m => m.FacilityId);
            builder.Ignore(m => m.Order);
            builder.Ignore(m => m.User);
        });

        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.HasOne(o => o.User).WithMany().HasForeignKey(o => o.UserId);
            builder.Ignore(o => o.Facility);
            builder.Ignore(o => o.Advisor);
            builder.Ignore(o => o.ShippingAddress);
            builder.Ignore(o => o.StatusHistory);
            builder.Ignore(o => o.Payments);
            builder.Ignore(o => o.Dispatch);
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.HasKey(oi => oi.Id);
            builder.HasOne(oi => oi.Order).WithMany(o => o.Items).HasForeignKey(oi => oi.OrderId);
            builder.HasOne(oi => oi.Product).WithMany().HasForeignKey(oi => oi.ProductId);
        });

        modelBuilder.Entity<Cart>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId);
        });

        modelBuilder.Entity<CartItem>(builder =>
        {
            builder.HasKey(ci => ci.Id);
            builder.HasOne(ci => ci.Cart).WithMany(c => c.Items).HasForeignKey(ci => ci.CartId);
            builder.HasOne(ci => ci.Product).WithMany().HasForeignKey(ci => ci.ProductId);
        });

        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.HasKey(rt => rt.Id);
            builder.HasOne(rt => rt.User).WithMany().HasForeignKey(rt => rt.UserId);
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
