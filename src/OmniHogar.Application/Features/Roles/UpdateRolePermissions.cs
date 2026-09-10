using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Constants;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Features.Roles;

/// <summary>Replaces the whole permission set of a role (HU-31 crit. 3).</summary>
public record UpdateRolePermissionsCommand(Guid RoleId, IReadOnlyList<string> Permissions) : IRequest;

public class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.RoleId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El rol es obligatorio.")
            .Must(id => id != SeededRoleIds.Administrador)
            .WithMessage("Los permisos del rol Administrador no se pueden modificar.")
            .MustAsync((id, ct) => context.Roles.AnyAsync(r => r.Id == id, ct))
            .WithMessage("El rol no existe.");

        RuleForEach(x => x.Permissions)
            .Must(name => AppPermissions.All.Contains(name))
            .WithMessage("Permiso desconocido: '{PropertyValue}'.");
    }
}

public class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateRolePermissionsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var current = await _context.RolePermissions
            .Where(rp => rp.RoleId == request.RoleId)
            .ToListAsync(cancellationToken);

        _context.RolePermissions.RemoveRange(current);

        var wanted = request.Permissions.Distinct().ToList();
        var permissionIds = await _context.Permissions
            .Where(p => wanted.Contains(p.Name))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        foreach (var permissionId in permissionIds)
        {
            _context.RolePermissions.Add(new RolePermission { RoleId = request.RoleId, PermissionId = permissionId });
        }

        await _context.SaveChangesAsync(cancellationToken);

        // "Aplicar al empleado": revoca los refresh tokens de quien tenga este rol para que
        // su próximo refresh falle y recoja los permisos nuevos al re-loguear.
        await RevokeTokensForRoleAsync(request.RoleId, cancellationToken);
    }

    private async Task RevokeTokensForRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var userIds = await _context.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync(cancellationToken);

        if (userIds.Count == 0)
        {
            return;
        }

        var tokens = await _context.RefreshTokens
            .Where(rt => userIds.Contains(rt.UserId) && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
