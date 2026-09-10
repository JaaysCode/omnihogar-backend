using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Constants;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Features.Employees;

/// <summary>Reassigns an existing employee to a different (single) role (HU-31 crit. 2).</summary>
public record ChangeEmployeeRoleCommand(Guid EmployeeId, Guid RoleId) : IRequest;

public class ChangeEmployeeRoleCommandValidator : AbstractValidator<ChangeEmployeeRoleCommand>
{
    public ChangeEmployeeRoleCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.RoleId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El rol es obligatorio.")
            .MustAsync((id, ct) => context.Roles.AnyAsync(r => r.Id == id, ct))
            .WithMessage("El rol no existe.");
    }
}

public class ChangeEmployeeRoleCommandHandler : IRequestHandler<ChangeEmployeeRoleCommand>
{
    private readonly IApplicationDbContext _context;

    public ChangeEmployeeRoleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ChangeEmployeeRoleCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == request.EmployeeId && u.UserType == UserType.employee, cancellationToken);

        if (employee is null)
        {
            throw NotFoundException.Empleado(request.EmployeeId);
        }

        var isAdminNow = employee.UserRoles.Any(ur => ur.RoleId == SeededRoleIds.Administrador);
        if (isAdminNow && request.RoleId != SeededRoleIds.Administrador)
        {
            var otherAdmins = await _context.UserRoles
                .CountAsync(ur => ur.RoleId == SeededRoleIds.Administrador && ur.UserId != employee.Id, cancellationToken);

            if (otherAdmins == 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["RoleId"] = ["Debe existir al menos un Administrador."],
                });
            }
        }

        _context.UserRoles.RemoveRange(employee.UserRoles);
        _context.UserRoles.Add(new UserRole { UserId = employee.Id, RoleId = request.RoleId });
        await _context.SaveChangesAsync(cancellationToken);

        // "Aplicar": revoca los refresh tokens del empleado para que recoja el nuevo rol
        // en el próximo re-login.
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == employee.Id && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
