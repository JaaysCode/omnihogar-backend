using Microsoft.EntityFrameworkCore;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the persistence layer. Implemented by Infrastructure's DbContext,
/// keeping Application ignorant of EF Core/Postgres specifics.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
