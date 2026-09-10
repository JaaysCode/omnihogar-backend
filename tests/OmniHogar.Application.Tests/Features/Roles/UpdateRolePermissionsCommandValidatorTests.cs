using FluentValidation.TestHelper;
using OmniHogar.Application.Features.Roles;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Constants;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Tests.Features.Roles;

public class UpdateRolePermissionsCommandValidatorTests
{
    private static async Task<InMemoryApplicationDbContext> ContextWithRole(Guid roleId, string name)
    {
        var context = InMemoryApplicationDbContext.Create();
        context.Roles.Add(new Role { Id = roleId, Name = name });
        await context.SaveChangesAsync(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task ValidRoleAndPermissions_HasNoValidationErrors()
    {
        var roleId = Guid.NewGuid();
        await using var context = await ContextWithRole(roleId, "Jefe de Bodega");
        var validator = new UpdateRolePermissionsCommandValidator(context);

        var result = await validator.TestValidateAsync(
            new UpdateRolePermissionsCommand(roleId, [AppPermissions.InventarioConsultar]));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task AdministradorRole_CannotBeModified()
    {
        await using var context = await ContextWithRole(SeededRoleIds.Administrador, "Administrador");
        var validator = new UpdateRolePermissionsCommandValidator(context);

        var result = await validator.TestValidateAsync(
            new UpdateRolePermissionsCommand(SeededRoleIds.Administrador, []));

        result.ShouldHaveValidationErrorFor(x => x.RoleId)
            .WithErrorMessage("Los permisos del rol Administrador no se pueden modificar.");
    }

    [Fact]
    public async Task UnknownRole_ReportsRoleDoesNotExist()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new UpdateRolePermissionsCommandValidator(context);

        var result = await validator.TestValidateAsync(
            new UpdateRolePermissionsCommand(Guid.NewGuid(), []));

        result.ShouldHaveValidationErrorFor(x => x.RoleId).WithErrorMessage("El rol no existe.");
    }

    [Fact]
    public async Task UnknownPermissionName_ReportsError()
    {
        var roleId = Guid.NewGuid();
        await using var context = await ContextWithRole(roleId, "Asesor de Tienda");
        var validator = new UpdateRolePermissionsCommandValidator(context);

        var result = await validator.TestValidateAsync(
            new UpdateRolePermissionsCommand(roleId, ["inventario.consultar", "no.existe"]));

        Assert.False(result.IsValid);
    }
}
