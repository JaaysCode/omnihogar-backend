using FluentValidation.TestHelper;
using OmniHogar.Application.Features.Products;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Tests.Features.Products;

public class UpdateProductCommandValidatorTests
{
    private static UpdateProductCommand ValidCommand(Guid id) =>
        new(id, "SKU-001", "Widget", "A widget.", null, 19.99m, null, "active");

    [Fact]
    public async Task ValidCommand_HasNoValidationErrors()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new UpdateProductCommandValidator(context);

        var result = await validator.TestValidateAsync(ValidCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task InvalidStatus_ReportsValidationError()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new UpdateProductCommandValidator(context);
        var command = ValidCommand(Guid.NewGuid()) with { Status = "unknown" };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public async Task SkuTakenByAnotherProduct_ReportsValidationError()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var other = new Product { Sku = "SKU-001", Name = "Existing", Price = 5m };
        context.Products.Add(other);
        await context.SaveChangesAsync(CancellationToken.None);
        var validator = new UpdateProductCommandValidator(context);

        var result = await validator.TestValidateAsync(ValidCommand(Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(x => x.Sku);
    }

    [Fact]
    public async Task SkuUnchangedOnSameProduct_HasNoValidationError()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var product = new Product { Sku = "SKU-001", Name = "Existing", Price = 5m };
        context.Products.Add(product);
        await context.SaveChangesAsync(CancellationToken.None);
        var validator = new UpdateProductCommandValidator(context);

        var result = await validator.TestValidateAsync(ValidCommand(product.Id));

        result.ShouldNotHaveValidationErrorFor(x => x.Sku);
    }
}
