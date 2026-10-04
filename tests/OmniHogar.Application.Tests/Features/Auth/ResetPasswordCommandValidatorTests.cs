using FluentValidation.TestHelper;
using OmniHogar.Application.Features.Auth;

namespace OmniHogar.Application.Tests.Features.Auth;

public class ResetPasswordCommandValidatorTests
{
    private static readonly ResetPasswordCommandValidator Validator = new();

    [Fact]
    public async Task ValidCommand_HasNoValidationErrors()
    {
        var result = await Validator.TestValidateAsync(new ResetPasswordCommand("some-token", "Passw0rd!"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task MissingToken_ReportsError()
    {
        var result = await Validator.TestValidateAsync(new ResetPasswordCommand("", "Passw0rd!"));

        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Theory]
    [InlineData("short1")]
    [InlineData("nodigitshere")]
    [InlineData("12345678")]
    public async Task InvalidPasswordFormat_ReportsFormatError(string password)
    {
        var result = await Validator.TestValidateAsync(new ResetPasswordCommand("some-token", password));

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
