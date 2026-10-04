using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Features.Auth;

/// <summary>
/// Validates the email is registered and active before starting password recovery (HU-15 crit.
/// 2 — an unregistered or inactive email must be told so, not silently accepted).
/// </summary>
public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.")
            .MustAsync(async (email, cancellationToken) =>
            {
                var normalized = email.Trim().ToLowerInvariant();
                return await context.Users.AnyAsync(u => u.Email.ToLower() == normalized && u.Status, cancellationToken);
            })
            .WithMessage("No existe una cuenta asociada a este correo.");
    }
}
