using FluentValidation.TestHelper;
using OmniHogar.Application.Features.Auth;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Auth;

public class ForgotPasswordCommandValidatorTests
{
    private static async Task<InMemoryApplicationDbContext> ContextWithUser(string email, bool status = true)
    {
        var context = InMemoryApplicationDbContext.Create();
        context.Users.Add(new User
        {
            Email = email,
            FirstName = "Jane",
            LastName = "Doe",
            UserType = UserType.customer,
            PasswordHash = "hash",
            Status = status,
        });
        await context.SaveChangesAsync(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task RegisteredEmail_HasNoValidationErrors()
    {
        await using var context = await ContextWithUser("jane.doe@example.com");
        var validator = new ForgotPasswordCommandValidator(context);

        var result = await validator.TestValidateAsync(new ForgotPasswordCommand("jane.doe@example.com"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UnregisteredEmail_ReportsNoAssociatedAccount()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new ForgotPasswordCommandValidator(context);

        var result = await validator.TestValidateAsync(new ForgotPasswordCommand("nobody@example.com"));

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("No existe una cuenta asociada a este correo.");
    }

    [Fact]
    public async Task InactiveUser_IsTreatedAsNotRegistered()
    {
        await using var context = await ContextWithUser("inactive@example.com", status: false);
        var validator = new ForgotPasswordCommandValidator(context);

        var result = await validator.TestValidateAsync(new ForgotPasswordCommand("inactive@example.com"));

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("No existe una cuenta asociada a este correo.");
    }

    [Fact]
    public async Task InvalidEmailFormat_ReportsFormatError()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new ForgotPasswordCommandValidator(context);

        var result = await validator.TestValidateAsync(new ForgotPasswordCommand("not-an-email"));

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
