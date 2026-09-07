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

        RuleFor(x => x.RoleId).NotEmpty().WithMessage("Role is required.");
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
