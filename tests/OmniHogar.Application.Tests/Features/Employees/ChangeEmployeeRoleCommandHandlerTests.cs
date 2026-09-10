using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.Employees;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Constants;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Tests.Features.Employees;

public class ChangeEmployeeRoleCommandHandlerTests
{
    private static User Employee(Guid id) =>
        new() { Id = id, UserType = UserType.employee, Email = $"{id:N}@x.test", FirstName = "E", LastName = "X", PasswordHash = "h" };

    [Fact]
    public async Task ReplacesTheEmployeesRole()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var employeeId = Guid.NewGuid();
        var oldRole = Guid.NewGuid();
        var newRole = Guid.NewGuid();
        context.Roles.AddRange(new Role { Id = oldRole, Name = "Asesor de Tienda" }, new Role { Id = newRole, Name = "Jefe de Bodega" });
        context.Users.Add(Employee(employeeId));
        context.UserRoles.Add(new UserRole { UserId = employeeId, RoleId = oldRole });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ChangeEmployeeRoleCommandHandler(context);
        await handler.Handle(new ChangeEmployeeRoleCommand(employeeId, newRole), CancellationToken.None);

        var roles = await context.UserRoles.Where(ur => ur.UserId == employeeId).ToListAsync();
        var single = Assert.Single(roles);
        Assert.Equal(newRole, single.RoleId);
    }

    [Fact]
    public async Task DemotingTheLastAdministrador_IsBlocked()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var employeeId = Guid.NewGuid();
        var otherRole = Guid.NewGuid();
        context.Roles.AddRange(
            new Role { Id = SeededRoleIds.Administrador, Name = "Administrador" },
            new Role { Id = otherRole, Name = "Asesor de Tienda" });
        context.Users.Add(Employee(employeeId));
        context.UserRoles.Add(new UserRole { UserId = employeeId, RoleId = SeededRoleIds.Administrador });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ChangeEmployeeRoleCommandHandler(context);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new ChangeEmployeeRoleCommand(employeeId, otherRole), CancellationToken.None));
    }

    [Fact]
    public async Task DemotingAnAdministrador_WhenAnotherExists_IsAllowed()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var otherRole = Guid.NewGuid();
        context.Roles.AddRange(
            new Role { Id = SeededRoleIds.Administrador, Name = "Administrador" },
            new Role { Id = otherRole, Name = "Asesor de Tienda" });
        context.Users.AddRange(Employee(a1), Employee(a2));
        context.UserRoles.AddRange(
            new UserRole { UserId = a1, RoleId = SeededRoleIds.Administrador },
            new UserRole { UserId = a2, RoleId = SeededRoleIds.Administrador });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ChangeEmployeeRoleCommandHandler(context);
        await handler.Handle(new ChangeEmployeeRoleCommand(a1, otherRole), CancellationToken.None);

        var roles = await context.UserRoles.Where(ur => ur.UserId == a1).ToListAsync();
        Assert.Equal(otherRole, Assert.Single(roles).RoleId);
    }

    [Fact]
    public async Task RevokesTheEmployeesRefreshTokens()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var employeeId = Guid.NewGuid();
        var oldRole = Guid.NewGuid();
        var newRole = Guid.NewGuid();
        context.Roles.AddRange(new Role { Id = oldRole, Name = "Asesor de Tienda" }, new Role { Id = newRole, Name = "Jefe de Bodega" });
        context.Users.Add(Employee(employeeId));
        context.UserRoles.Add(new UserRole { UserId = employeeId, RoleId = oldRole });
        context.RefreshTokens.Add(new RefreshToken { UserId = employeeId, TokenHash = "abc", ExpiresAt = DateTime.UtcNow.AddDays(7) });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ChangeEmployeeRoleCommandHandler(context);
        await handler.Handle(new ChangeEmployeeRoleCommand(employeeId, newRole), CancellationToken.None);

        var token = await context.RefreshTokens.SingleAsync(rt => rt.UserId == employeeId);
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public async Task UnknownEmployee_ThrowsNotFound()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var roleId = Guid.NewGuid();
        context.Roles.Add(new Role { Id = roleId, Name = "Jefe de Bodega" });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ChangeEmployeeRoleCommandHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ChangeEmployeeRoleCommand(Guid.NewGuid(), roleId), CancellationToken.None));
    }
}
