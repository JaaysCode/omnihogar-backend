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
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(150).WithMessage("Email must be at most 150 characters.")
            .MustAsync(async (email, cancellationToken) =>
            {
                var normalized = email.Trim().ToLowerInvariant();
                return !await context.Users.AnyAsync(u => u.Email.ToLower() == normalized, cancellationToken);
            })
            .WithMessage("Email is already registered.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Za-z]").WithMessage("Password must include at least one letter.")
            .Matches("[0-9]").WithMessage("Password must include at least one number.");

        RuleFor(x => x.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must be at most 100 characters.")
            .Matches(NamePattern).WithMessage("First name can only contain letters.");

        RuleFor(x => x.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must be at most 100 characters.")
            .Matches(NamePattern).WithMessage("Last name can only contain letters.");

        RuleFor(x => x.Phone)
            .Matches(PhonePattern).WithMessage("Phone format is invalid.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
