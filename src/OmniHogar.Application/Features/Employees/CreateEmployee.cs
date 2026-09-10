using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Features.Employees;

public record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Phone,
    Guid RoleId) : IRequest<Guid>;

/// <summary>
/// Mirrors <see cref="Auth.RegisterCommandValidator"/>'s name/phone/password rules
/// (kept in sync with the frontend's shared/utils/auth-validators.ts) plus the
/// RoleId requirement that's specific to employee accounts.
/// </summary>
public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    private static readonly Regex NamePattern = new(@"^[\p{L}\p{M}\s'-]+$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^\+?[0-9\s()-]{7,20}$", RegexOptions.Compiled);

    public CreateEmployeeCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.")
            .MaximumLength(150).WithMessage("El correo electrónico debe tener como máximo 150 caracteres.")
            .MustAsync(async (email, cancellationToken) =>
            {
                var normalized = email.Trim().ToLowerInvariant();
                return !await context.Users.AnyAsync(u => u.Email.ToLower() == normalized, cancellationToken);
            })
            .WithMessage("El correo ya está registrado.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .Matches("[A-Za-z]").WithMessage("La contraseña debe incluir al menos una letra.")
            .Matches("[0-9]").WithMessage("La contraseña debe incluir al menos un número.");

        RuleFor(x => x.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre debe tener como máximo 100 caracteres.")
            .Matches(NamePattern).WithMessage("El nombre solo puede contener letras.");

        RuleFor(x => x.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El apellido es obligatorio.")
            .MaximumLength(100).WithMessage("El apellido debe tener como máximo 100 caracteres.")
            .Matches(NamePattern).WithMessage("El apellido solo puede contener letras.");

        RuleFor(x => x.Phone)
            .Matches(PhonePattern).WithMessage("El formato del teléfono no es válido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.RoleId).NotEmpty().WithMessage("El rol es obligatorio.");
    }
}

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public CreateEmployeeCommandHandler(IApplicationDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var roleExists = await _context.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists)
        {
            throw new NotFoundException(nameof(Role), request.RoleId);
        }

        var employee = new User
        {
            UserType = UserType.employee,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
        };
        employee.PasswordHash = _passwordHasher.HashPassword(employee, request.Password);

        _context.Users.Add(employee);
        _context.UserRoles.Add(new UserRole { User = employee, RoleId = request.RoleId });

        await _context.SaveChangesAsync(cancellationToken);

        return employee.Id;
    }
}
