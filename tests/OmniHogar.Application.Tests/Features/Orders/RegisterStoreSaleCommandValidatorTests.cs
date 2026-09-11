using FluentValidation.TestHelper;
using OmniHogar.Application.Features.Orders;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Application.Tests.Features.Orders;

public class RegisterStoreSaleCommandValidatorTests
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
        var validator = new RegisterStoreSaleCommandValidator(context);

        var result = await validator.TestValidateAsync(
            new RegisterStoreSaleCommand([new StoreSaleItem(productId, 2)]));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task EmptyItems_ReportsError()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new RegisterStoreSaleCommandValidator(context);

        var result = await validator.TestValidateAsync(new RegisterStoreSaleCommand([]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Agrega al menos un producto a la venta.");
    }

    [Fact]
    public async Task ZeroQuantity_ReportsError()
    {
        var productId = Guid.NewGuid();
        await using var context = await ContextWithProduct(productId, "active");
        var validator = new RegisterStoreSaleCommandValidator(context);

        var result = await validator.TestValidateAsync(
            new RegisterStoreSaleCommand([new StoreSaleItem(productId, 0)]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "La cantidad debe ser mayor que cero.");
    }

    [Fact]
    public async Task UnknownProduct_ReportsUnavailable()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var validator = new RegisterStoreSaleCommandValidator(context);

        var result = await validator.TestValidateAsync(
            new RegisterStoreSaleCommand([new StoreSaleItem(Guid.NewGuid(), 1)]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Uno o más productos no están disponibles.");
    }

    [Fact]
    public async Task DiscontinuedProduct_ReportsUnavailable()
    {
        var productId = Guid.NewGuid();
        await using var context = await ContextWithProduct(productId, "discontinued");
        var validator = new RegisterStoreSaleCommandValidator(context);

        var result = await validator.TestValidateAsync(
            new RegisterStoreSaleCommand([new StoreSaleItem(productId, 1)]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Uno o más productos no están disponibles.");
    }
}
