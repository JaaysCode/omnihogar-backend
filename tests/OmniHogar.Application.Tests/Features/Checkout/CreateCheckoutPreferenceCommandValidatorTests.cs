using FluentValidation.TestHelper;
using OmniHogar.Application.Features.Checkout;

namespace OmniHogar.Application.Tests.Features.Checkout;

public class CreateCheckoutPreferenceCommandValidatorTests
{
    private static readonly CreateCheckoutPreferenceCommandValidator Validator = new();

    private static CustomerAddressInput ValidAddress() => new("Calle 123 #45-67", "Bogotá", "Chapinero", "Casa azul");

    [Fact]
    public async Task ValidAddressAndPaymentMethod_HasNoValidationErrors()
    {
        var result = await Validator.TestValidateAsync(new CreateCheckoutPreferenceCommand(ValidAddress(), "card"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task EmptyAddress_ReportsError()
    {
        var result = await Validator.TestValidateAsync(
            new CreateCheckoutPreferenceCommand(ValidAddress() with { Address = "" }, "card"));

        result.ShouldHaveValidationErrorFor("Address.Address").WithErrorMessage("La dirección es obligatoria.");
    }

    [Fact]
    public async Task EmptyCity_ReportsError()
    {
        var result = await Validator.TestValidateAsync(
            new CreateCheckoutPreferenceCommand(ValidAddress() with { City = "" }, "card"));

        result.ShouldHaveValidationErrorFor("Address.City").WithErrorMessage("La ciudad es obligatoria.");
    }

    [Fact]
    public async Task EmptyPaymentMethod_ReportsError()
    {
        var result = await Validator.TestValidateAsync(new CreateCheckoutPreferenceCommand(ValidAddress(), ""));

        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod).WithErrorMessage("Selecciona un método de pago.");
    }

    [Fact]
    public async Task UnknownPaymentMethod_ReportsError()
    {
        var result = await Validator.TestValidateAsync(new CreateCheckoutPreferenceCommand(ValidAddress(), "bitcoin"));

        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod).WithErrorMessage("El método de pago no es válido.");
    }
}
