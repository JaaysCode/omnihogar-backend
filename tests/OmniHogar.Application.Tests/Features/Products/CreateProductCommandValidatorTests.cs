using FluentValidation.TestHelper;
using OmniHogar.Application.Features.Products;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Tests.Features.Products;

public class CreateProductCommandValidatorTests
{
    private static CreateProductCommand ValidCommand() =>
        new("SKU-001", "Widget", "A widget.", null, 19.99m, null);

    [Fact]
    public async Task ValidCommand_HasNoValidationErrors()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new CreateProductCommandValidator(context);

        var result = await validator.TestValidateAsync(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "Widget")]
    [InlineData("SKU-001", "")]
    public async Task MissingMandatoryField_ReportsWhichFieldMustBeCompleted(string sku, string name)
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new CreateProductCommandValidator(context);
        var command = ValidCommand() with { Sku = sku, Name = name };

        var result = await validator.TestValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task NegativePrice_ReportsValidationError()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new CreateProductCommandValidator(context);
        var command = ValidCommand() with { Price = -1m };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public async Task DuplicateSku_ReportsValidationError()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        context.Products.Add(new Product { Sku = "SKU-001", Name = "Existing", Price = 5m });
        await context.SaveChangesAsync(CancellationToken.None);
        var validator = new CreateProductCommandValidator(context);

        var result = await validator.TestValidateAsync(ValidCommand());

        result.ShouldHaveValidationErrorFor(x => x.Sku);
    }
}
