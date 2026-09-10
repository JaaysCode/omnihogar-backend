using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.Roles;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Constants;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Roles;

public class UpdateRolePermissionsCommandHandlerTests
{
    [Fact]
    public async Task ReplacesTheRolesPermissionSet()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var roleId = Guid.NewGuid();
        context.Roles.Add(new Role { Id = roleId, Name = "Jefe de Bodega" });
        context.Permissions.Add(new Permission { Id = SeededPermissionIds.InventarioConsultar, Name = AppPermissions.InventarioConsultar });
        context.Permissions.Add(new Permission { Id = SeededPermissionIds.InventarioAjustar, Name = AppPermissions.InventarioAjustar });
        context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = SeededPermissionIds.InventarioAjustar });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateRolePermissionsCommandHandler(context);
        await handler.Handle(
            new UpdateRolePermissionsCommand(roleId, [AppPermissions.InventarioConsultar]),
            CancellationToken.None);

        var remaining = await context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        var single = Assert.Single(remaining);
        Assert.Equal(SeededPermissionIds.InventarioConsultar, single.PermissionId);
    }

    [Fact]
    public async Task RevokesRefreshTokensOfEmployeesHoldingTheRole()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var roleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        context.Roles.Add(new Role { Id = roleId, Name = "Coordinador de Despacho" });
        context.Permissions.Add(new Permission { Id = SeededPermissionIds.InventarioConsultar, Name = AppPermissions.InventarioConsultar });
        context.Users.Add(new User { Id = userId, UserType = UserType.employee, Email = "e@x.test", FirstName = "E", LastName = "X", PasswordHash = "h" });
        context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        context.RefreshTokens.Add(new RefreshToken { UserId = userId, TokenHash = "abc", ExpiresAt = DateTime.UtcNow.AddDays(7) });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateRolePermissionsCommandHandler(context);
        await handler.Handle(new UpdateRolePermissionsCommand(roleId, []), CancellationToken.None);

        var token = await context.RefreshTokens.SingleAsync(rt => rt.UserId == userId);
        Assert.NotNull(token.RevokedAt);
    }
}
