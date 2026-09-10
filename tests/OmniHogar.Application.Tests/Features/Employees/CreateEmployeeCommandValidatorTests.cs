using FluentValidation.TestHelper;
using OmniHogar.Application.Features.Employees;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Employees;

public class CreateEmployeeCommandValidatorTests
{
    private static CreateEmployeeCommand ValidCommand() =>
        new("Jane", "Doe", "jane.doe@example.com", "Passw0rd!", "+57 300 1234567", Guid.NewGuid());

    [Fact]
    public async Task ValidCommand_HasNoValidationErrors()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new CreateEmployeeCommandValidator(context);

        var result = await validator.TestValidateAsync(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "Doe", "jane.doe@example.com", "Passw0rd!")]
    [InlineData("Jane", "", "jane.doe@example.com", "Passw0rd!")]
    [InlineData("Jane", "Doe", "", "Passw0rd!")]
    [InlineData("Jane", "Doe", "jane.doe@example.com", "")]
    public async Task MissingMandatoryField_ReportsInvalid(string first, string last, string email, string password)
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new CreateEmployeeCommandValidator(context);
        var command = new CreateEmployeeCommand(first, last, email, password, null, Guid.NewGuid());

        var result = await validator.TestValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task MissingRole_ReportsRoleRequired()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new CreateEmployeeCommandValidator(context);
        var command = ValidCommand() with { RoleId = Guid.Empty };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.RoleId)
            .WithErrorMessage("El rol es obligatorio.");
    }

    [Fact]
    public async Task DuplicateEmail_ReportsAlreadyRegistered_InSpanish()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        context.Users.Add(new User
        {
            Email = "jane.doe@example.com",
            FirstName = "Existing",
            LastName = "User",
            UserType = UserType.employee,
            PasswordHash = "hash",
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateEmployeeCommandValidator(context);

        var result = await validator.TestValidateAsync(ValidCommand());

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("El correo ya está registrado.");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    public async Task InvalidEmailFormat_ReportsFormatError(string email)
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new CreateEmployeeCommandValidator(context);
        var command = ValidCommand() with { Email = email };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task InvalidFirstNameFormat_ReportsFormatError()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new CreateEmployeeCommandValidator(context);
        var command = ValidCommand() with { FirstName = "Jane123" };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }
}
