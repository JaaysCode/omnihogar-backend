using FluentValidation.TestHelper;
using OmniHogar.Application.Features.Profile;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Profile;

public class UpdateProfileCommandValidatorTests
{
    private static UpdateProfileCommand ValidCommand() => new("Ana", "Gómez", "ana@x.test", "+57 300 1234567");

    [Fact]
    public async Task ValidCommand_HasNoValidationErrors()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new UpdateProfileCommandValidator(context, new FakeCurrentUserService());

        var result = await validator.TestValidateAsync(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task NoPhone_IsAllowed()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new UpdateProfileCommandValidator(context, new FakeCurrentUserService());

        var result = await validator.TestValidateAsync(ValidCommand() with { Phone = null });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "Gómez", "ana@x.test")]
    [InlineData("Ana", "", "ana@x.test")]
    [InlineData("Ana", "Gómez", "")]
    public async Task MissingMandatoryField_ReportsInvalid(string first, string last, string email)
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new UpdateProfileCommandValidator(context, new FakeCurrentUserService());

        var result = await validator.TestValidateAsync(new UpdateProfileCommand(first, last, email, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task InvalidFirstNameFormat_ReportsFormatError()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new UpdateProfileCommandValidator(context, new FakeCurrentUserService());

        var result = await validator.TestValidateAsync(ValidCommand() with { FirstName = "Ana123" });

        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("El nombre solo puede contener letras.");
    }

    [Fact]
    public async Task InvalidLastNameFormat_ReportsFormatError()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new UpdateProfileCommandValidator(context, new FakeCurrentUserService());

        var result = await validator.TestValidateAsync(ValidCommand() with { LastName = "Gómez!" });

        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("El apellido solo puede contener letras.");
    }

    [Fact]
    public async Task InvalidEmailFormat_ReportsFormatError()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new UpdateProfileCommandValidator(context, new FakeCurrentUserService());

        var result = await validator.TestValidateAsync(ValidCommand() with { Email = "not-an-email" });

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("El formato del correo electrónico no es válido.");
    }

    [Fact]
    public async Task InvalidPhoneFormat_ReportsFormatError()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new UpdateProfileCommandValidator(context, new FakeCurrentUserService());

        var result = await validator.TestValidateAsync(ValidCommand() with { Phone = "abc" });

        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("El formato del teléfono no es válido.");
    }

    [Fact]
    public async Task EmailBelongingToAnotherUser_ReportsAlreadyRegistered()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        context.Users.Add(new User
        {
            UserType = UserType.customer,
            FirstName = "Otra",
            LastName = "Persona",
            Email = "ana@x.test",
            PasswordHash = "h",
        });
        await context.SaveChangesAsync(CancellationToken.None);
        var validator = new UpdateProfileCommandValidator(context, new FakeCurrentUserService());

        var result = await validator.TestValidateAsync(ValidCommand());

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("El correo ya está registrado.");
    }

    [Fact]
    public async Task ReSubmittingOwnCurrentEmail_IsAllowed()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            UserType = UserType.customer,
            FirstName = "Ana",
            LastName = "Gómez",
            Email = "ana@x.test",
            PasswordHash = "h",
        });
        await context.SaveChangesAsync(CancellationToken.None);
        var validator = new UpdateProfileCommandValidator(context, new FakeCurrentUserService { UserId = userId.ToString() });

        var result = await validator.TestValidateAsync(ValidCommand());

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }
}
