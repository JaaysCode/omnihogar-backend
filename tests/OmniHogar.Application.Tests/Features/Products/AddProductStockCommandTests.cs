using FluentValidation.TestHelper;
using OmniHogar.Application.Features.Products;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Tests.Features.Products;

public class AddProductStockCommandValidatorTests
{
    private readonly AddProductStockCommandValidator _validator = new();

    [Fact]
    public async Task PositiveQuantity_HasNoValidationErrors()
    {
        var result = await _validator.TestValidateAsync(new AddProductStockCommand(Guid.NewGuid(), 10, "Reposición"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task NonPositiveQuantity_ReportsValidationError(int quantity)
    {
        var result = await _validator.TestValidateAsync(new AddProductStockCommand(Guid.NewGuid(), quantity, null));
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public async Task ReasonTooLong_ReportsValidationError()
    {
        var command = new AddProductStockCommand(Guid.NewGuid(), 5, new string('x', 256));
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }
}

public class AddProductStockCommandHandlerTests
{
    private static Facility DefaultFacility() => new() { Name = "Bodega Central", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" };

    [Fact]
    public async Task NewProduct_CreatesInventoryRow_AndLogsMovement()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var product = new Product { Sku = "SKU-001", Name = "Aspiradora Robot", Price = 899_900m };
        var facility = DefaultFacility();
        context.Products.Add(product);
        context.Facilities.Add(facility);
        await context.SaveChangesAsync(CancellationToken.None);

        var currentUser = new FakeCurrentUserService();
        var handler = new AddProductStockCommandHandler(context, currentUser);
        await handler.Handle(new AddProductStockCommand(product.Id, 15, "Reposición inicial"), CancellationToken.None);

        var inventory = Assert.Single(context.Inventory);
        Assert.Equal(15, inventory.AvailableQuantity);
        Assert.Equal(facility.Id, inventory.FacilityId);

        var movement = Assert.Single(context.InventoryMovements);
        Assert.Equal("inbound", movement.Type);
        Assert.Equal(15, movement.Quantity);
        Assert.Equal("Reposición inicial", movement.Reason);
        Assert.Equal(currentUser.UserId, movement.UserId.ToString());
    }

    [Fact]
    public async Task ExistingInventoryRow_IncrementsQuantity_InsteadOfDuplicating()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var product = new Product { Sku = "SKU-002", Name = "Taladro", Price = 250_000m };
        var facility = DefaultFacility();
        context.Products.Add(product);
        context.Facilities.Add(facility);
        context.Inventory.Add(new Inventory { ProductId = product.Id, FacilityId = facility.Id, AvailableQuantity = 5 });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AddProductStockCommandHandler(context, new FakeCurrentUserService());
        await handler.Handle(new AddProductStockCommand(product.Id, 20, null), CancellationToken.None);

        var inventory = Assert.Single(context.Inventory);
        Assert.Equal(25, inventory.AvailableQuantity);
    }

    [Fact]
    public async Task NonexistentProduct_ThrowsNotFoundException()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        context.Facilities.Add(DefaultFacility());
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AddProductStockCommandHandler(context, new FakeCurrentUserService());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new AddProductStockCommand(Guid.NewGuid(), 5, null), CancellationToken.None));
    }

    [Fact]
    public async Task NoFacilityConfigured_ThrowsValidationException()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var product = new Product { Sku = "SKU-003", Name = "Ventilador", Price = 120_000m };
        context.Products.Add(product);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AddProductStockCommandHandler(context, new FakeCurrentUserService());

        await Assert.ThrowsAsync<OmniHogar.Domain.Exceptions.ValidationException>(
            () => handler.Handle(new AddProductStockCommand(product.Id, 5, null), CancellationToken.None));
    }
}
