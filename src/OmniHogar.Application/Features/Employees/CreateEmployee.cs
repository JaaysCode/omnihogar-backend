using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Exceptions;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Features.Employees;

public record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Phone,
    Guid RoleId) : IRequest<Guid>;

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.RoleId).NotEmpty();
    }
}

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public CreateEmployeeCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var emailTaken = await _context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);
        if (emailTaken)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(request.Email)] = ["El correo ya está registrado."],
            });
        }

        var roleExists = await _context.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists)
        {
            throw new NotFoundException(nameof(Role), request.RoleId);
        }

        var employee = new User
        {
            UserType = "employee",
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
        };
        employee.PasswordHash = _passwordHasher.Hash(request.Password);

        _context.Users.Add(employee);
        _context.UserRoles.Add(new UserRole { User = employee, RoleId = request.RoleId });

        await _context.SaveChangesAsync(cancellationToken);

        return employee.Id;
    }
}
