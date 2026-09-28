using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Features.Profile;

/// <summary>Updates the current user's own name/email/phone (HU-16 crit. 2: "Actualizar
/// datos"). Email change here is unverified — swaps the login identifier immediately, no
/// confirmation email — same trust level as the rest of the account's own-data edits.</summary>
public record UpdateProfileCommand(string FirstName, string LastName, string Email, string? Phone) : IRequest<ProfileDto>;

/// <summary>Mirrors <see cref="Auth.RegisterCommandValidator"/>'s name/email/phone rules (HU-16
/// crit. 3: "Datos inválidos" — informa los campos a corregir y no guarda nada inválido). Email
/// uniqueness excludes the current user's own row, so re-submitting the same address is allowed.</summary>
public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    private static readonly Regex NamePattern = new(@"^[\p{L}\p{M}\s'-]+$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^\+?[0-9\s()-]{7,20}$", RegexOptions.Compiled);

    public UpdateProfileCommandValidator(IApplicationDbContext context, ICurrentUserService currentUser)
    {
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

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.")
            .MaximumLength(150).WithMessage("El correo electrónico debe tener como máximo 150 caracteres.")
            .MustAsync(async (email, cancellationToken) =>
            {
                var normalized = email.Trim().ToLowerInvariant();
                var userId = Guid.Parse(currentUser.UserId!);
                return !await context.Users.AnyAsync(u => u.Email.ToLower() == normalized && u.Id != userId, cancellationToken);
            })
            .WithMessage("El correo ya está registrado.");

        RuleFor(x => x.Phone)
            .Matches(PhonePattern).WithMessage("El formato del teléfono no es válido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, ProfileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateProfileCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUser.UserId!);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            throw NotFoundException.Usuario(userId);
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = request.Email.Trim().ToLowerInvariant();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return new ProfileDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            CreatedAt = user.CreatedAt,
        };
    }
}
