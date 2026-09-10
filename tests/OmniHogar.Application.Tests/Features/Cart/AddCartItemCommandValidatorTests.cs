using FluentValidation.TestHelper;
using OmniHogar.Application.Features.Cart;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Tests.Features.Cart;

public class AddCartItemCommandValidatorTests
{
    private static async Task<InMemoryApplicationDbContext> ContextWithProduct(Guid productId, string status)
    {
        var context = InMemoryApplicationDbContext.Create();
        context.Products.Add(new Product { Id = productId, Sku = "SKU-1", Name = "Silla", Price = 100m, Status = status });
        await context.SaveChangesAsync(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task ActiveProductAndPositiveQuantity_HasNoValidationErrors()
    {
        var productId = Guid.NewGuid();
        await using var context = await ContextWithProduct(productId, "active");
        var validator = new AddCartItemCommandValidator(context);

        var result = await validator.TestValidateAsync(new AddCartItemCommand(productId, 2));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ZeroQuantity_ReportsError()
    {
        var productId = Guid.NewGuid();
        await using var context = await ContextWithProduct(productId, "active");
        var validator = new AddCartItemCommandValidator(context);

        var result = await validator.TestValidateAsync(new AddCartItemCommand(productId, 0));

        result.ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage("La cantidad debe ser mayor que cero.");
    }

    [Fact]
    public async Task UnknownProduct_ReportsUnavailable()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new AddCartItemCommandValidator(context);

        var result = await validator.TestValidateAsync(new AddCartItemCommand(Guid.NewGuid(), 1));

        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage("El producto no está disponible.");
    }

    [Fact]
    public async Task DiscontinuedProduct_ReportsUnavailable()
    {
        var productId = Guid.NewGuid();
        await using var context = await ContextWithProduct(productId, "discontinued");
        var validator = new AddCartItemCommandValidator(context);

        var result = await validator.TestValidateAsync(new AddCartItemCommand(productId, 1));

        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage("El producto no está disponible.");
    }
}
