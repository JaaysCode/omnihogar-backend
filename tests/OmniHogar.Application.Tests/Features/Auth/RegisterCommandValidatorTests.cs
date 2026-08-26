using FluentValidation.TestHelper;
using OmniHogar.Application.Features.Auth;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Tests.Features.Auth;

public class RegisterCommandValidatorTests
{
    private static RegisterCommand ValidCommand() =>
        new("jane.doe@example.com", "Passw0rd!", "Jane", "Doe", "+57 300 1234567");

    [Fact]
    public async Task ValidCommand_HasNoValidationErrors()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new RegisterCommandValidator(context);

        var result = await validator.TestValidateAsync(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "Passw0rd!", "Jane", "Doe")]
    [InlineData("jane.doe@example.com", "", "Jane", "Doe")]
    [InlineData("jane.doe@example.com", "Passw0rd!", "", "Doe")]
    [InlineData("jane.doe@example.com", "Passw0rd!", "Jane", "")]
    public async Task MissingMandatoryField_ReportsWhichFieldMustBeCompleted(
        string email, string password, string firstName, string lastName)
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new RegisterCommandValidator(context);
        var command = new RegisterCommand(email, password, firstName, lastName, null);

        var result = await validator.TestValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task DuplicateEmail_ReportsEmailAlreadyRegistered_AndDoesNotBlockOtherFields()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        context.Users.Add(new User
        {
            Email = "jane.doe@example.com",
            FirstName = "Existing",
            LastName = "User",
            UserType = "customer",
            PasswordHash = "hash",
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new RegisterCommandValidator(context);

        var result = await validator.TestValidateAsync(ValidCommand());

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("El correo ya está registrado.");
    }

    [Fact]
    public async Task DuplicateEmail_IsCaseInsensitive()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        context.Users.Add(new User
        {
            Email = "jane.doe@example.com",
            FirstName = "Existing",
            LastName = "User",
            UserType = "customer",
            PasswordHash = "hash",
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new RegisterCommandValidator(context);
        var command = ValidCommand() with { Email = "JANE.DOE@EXAMPLE.COM" };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    public async Task InvalidEmailFormat_ReportsFormatError(string email)
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new RegisterCommandValidator(context);
        var command = ValidCommand() with { Email = email };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("short1")]
    [InlineData("nodigitshere")]
    [InlineData("12345678")]
    public async Task InvalidPasswordFormat_ReportsFormatError(string password)
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new RegisterCommandValidator(context);
        var command = ValidCommand() with { Password = password };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public async Task InvalidFirstNameFormat_ReportsFormatError()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new RegisterCommandValidator(context);
        var command = ValidCommand() with { FirstName = "Jane123" };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public async Task InvalidPhoneFormat_ReportsFormatError()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new RegisterCommandValidator(context);
        var command = ValidCommand() with { Phone = "not-a-phone!!" };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public async Task MissingOptionalPhone_HasNoValidationError()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new RegisterCommandValidator(context);
        var command = ValidCommand() with { Phone = null };

        var result = await validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }
}
