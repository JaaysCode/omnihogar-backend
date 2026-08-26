using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Features.Auth;

/// <summary>
/// Validates mandatory fields, format, and email uniqueness for <see cref="RegisterCommand"/>
/// before a new customer account is created. Uniqueness is re-checked at the DB level (unique
/// index on users.email) as a race-condition safety net.
/// </summary>
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    private static readonly Regex NamePattern = new(@"^[\p{L}\p{M}\s'-]+$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^\+?[0-9\s()-]{7,20}$", RegexOptions.Compiled);

    public RegisterCommandValidator(IApplicationDbContext context)
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
    }
}
