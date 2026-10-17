using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.InventoryTransfers;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Tests.Features.InventoryTransfers;

public class CreateInventoryTransferCommandValidatorTests
{
    private readonly CreateInventoryTransferCommandValidator _validator = new();

    [Fact]
    public async Task ValidCommand_HasNoErrors()
    {
        var result = await _validator.TestValidateAsync(
            new CreateInventoryTransferCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task NonPositiveQuantity_IsRejected(int quantity)
    {
        var result = await _validator.TestValidateAsync(
            new CreateInventoryTransferCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), quantity));
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public async Task SameOriginAndDestination_IsRejected()
    {
        var facility = Guid.NewGuid();
        var result = await _validator.TestValidateAsync(
            new CreateInventoryTransferCommand(Guid.NewGuid(), facility, facility, 5));
        result.ShouldHaveValidationErrorFor(x => x.DestinationFacilityId);
    }
}

public class InventoryTransferHandlerTests
{
    private static readonly FakeCurrentUserService User = new();

    private record Seeded(InMemoryApplicationDbContext Context, Product Product, Facility Warehouse, Facility Store, Inventory WarehouseStock);

    private static async Task<Seeded> Seed(int warehouseUnits)
    {
        var context = InMemoryApplicationDbContext.Create();
        var product = new Product { Sku = "SKU-1", Name = "Aspiradora Robot", Price = 899_900m };
        var warehouse = new Facility { Name = "Bodega Central", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" };
        var store = new Facility { Name = "Tienda Norte", Type = FacilityType.POS, Address = "Calle 2", City = "Bogotá" };
        var stock = new Inventory { ProductId = product.Id, FacilityId = warehouse.Id, AvailableQuantity = warehouseUnits };
        context.Products.Add(product);
        context.Facilities.AddRange(warehouse, store);
        context.Inventory.Add(stock);
        await context.SaveChangesAsync(CancellationToken.None);
        return new Seeded(context, product, warehouse, store, stock);
    }

    // --- Registrar transferencia ---

    [Fact]
    public async Task Create_WithAvailableUnits_CreatesPendingTransferWithTheGivenData()
    {
        var s = await Seed(10);
        var handler = new CreateInventoryTransferCommandHandler(s.Context);

        var result = await handler.Handle(
            new CreateInventoryTransferCommand(s.Product.Id, s.Warehouse.Id, s.Store.Id, 4), CancellationToken.None);

        Assert.Equal("pending", result.Status);
        Assert.Equal(4, result.Quantity);
        Assert.Equal("Bodega Central", result.SourceFacilityName);
        Assert.Equal("Tienda Norte", result.DestinationFacilityName);
        Assert.Equal("Aspiradora Robot", result.ProductName);
        var persisted = await s.Context.InventoryTransfers.SingleAsync();
        Assert.Equal(s.Product.Id, persisted.ProductId);
        Assert.Equal(s.Warehouse.Id, persisted.SourceFacilityId);
        Assert.Equal(s.Store.Id, persisted.DestinationFacilityId);
    }

    [Fact]
    public async Task Create_DoesNotTouchStockUntilConfirmed()
    {
        var s = await Seed(10);
        var handler = new CreateInventoryTransferCommandHandler(s.Context);

        await handler.Handle(new CreateInventoryTransferCommand(s.Product.Id, s.Warehouse.Id, s.Store.Id, 4), CancellationToken.None);

        Assert.Equal(10, (await s.Context.Inventory.SingleAsync()).AvailableQuantity);
        Assert.Empty(s.Context.InventoryMovements);
    }

    // --- Cantidad disponible ---

    [Fact]
    public async Task Create_QuantityAboveAvailable_IsRejectedWithInsufficientUnitsMessage()
    {
        var s = await Seed(3);
        var handler = new CreateInventoryTransferCommandHandler(s.Context);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new CreateInventoryTransferCommand(s.Product.Id, s.Warehouse.Id, s.Store.Id, 4), CancellationToken.None));

        Assert.Contains("No hay unidades suficientes", ex.Errors["quantity"][0]);
        Assert.Empty(s.Context.InventoryTransfers);
    }

    [Fact]
    public async Task Create_OriginWithoutInventoryRow_IsRejectedAsInsufficient()
    {
        var s = await Seed(10);
        var handler = new CreateInventoryTransferCommandHandler(s.Context);

        // The store has no inventory row for this product -> 0 available.
        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new CreateInventoryTransferCommand(s.Product.Id, s.Store.Id, s.Warehouse.Id, 1), CancellationToken.None));

        Assert.Contains("No hay unidades suficientes", ex.Errors["quantity"][0]);
    }

    [Fact]
    public async Task Create_ExactlyAllAvailableUnits_IsAllowed()
    {
        var s = await Seed(3);
        var handler = new CreateInventoryTransferCommandHandler(s.Context);

        var result = await handler.Handle(
            new CreateInventoryTransferCommand(s.Product.Id, s.Warehouse.Id, s.Store.Id, 3), CancellationToken.None);

        Assert.Equal(3, result.Quantity);
    }

    [Fact]
    public async Task Create_UnknownProduct_ThrowsNotFound()
    {
        var s = await Seed(3);
        var handler = new CreateInventoryTransferCommandHandler(s.Context);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new CreateInventoryTransferCommand(Guid.NewGuid(), s.Warehouse.Id, s.Store.Id, 1), CancellationToken.None));
    }

    [Fact]
    public async Task Create_UnknownFacility_ReportsFieldError()
    {
        var s = await Seed(3);
        var handler = new CreateInventoryTransferCommandHandler(s.Context);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new CreateInventoryTransferCommand(s.Product.Id, s.Warehouse.Id, Guid.NewGuid(), 1), CancellationToken.None));

        Assert.True(ex.Errors.ContainsKey("destinationFacilityId"));
    }

    // --- Actualizar origen ---

    [Fact]
    public async Task Confirm_DeductsTransferredUnitsFromOrigin_AndMarksInTransit()
    {
        var s = await Seed(10);
        var created = await new CreateInventoryTransferCommandHandler(s.Context).Handle(
            new CreateInventoryTransferCommand(s.Product.Id, s.Warehouse.Id, s.Store.Id, 4), CancellationToken.None);
        var handler = new ConfirmInventoryTransferCommandHandler(s.Context, User);

        var result = await handler.Handle(new ConfirmInventoryTransferCommand(created.Id), CancellationToken.None);

        Assert.Equal("in_transit", result.Status);
        var origin = await s.Context.Inventory.SingleAsync(i => i.FacilityId == s.Warehouse.Id);
        Assert.Equal(6, origin.AvailableQuantity);
    }

    [Fact]
    public async Task Confirm_LogsTransferMovementAgainstTheOrigin()
    {
        var s = await Seed(10);
        var created = await new CreateInventoryTransferCommandHandler(s.Context).Handle(
            new CreateInventoryTransferCommand(s.Product.Id, s.Warehouse.Id, s.Store.Id, 4), CancellationToken.None);
        var userId = Guid.NewGuid();
        var handler = new ConfirmInventoryTransferCommandHandler(s.Context, new FakeCurrentUserService { UserId = userId.ToString() });

        await handler.Handle(new ConfirmInventoryTransferCommand(created.Id), CancellationToken.None);

        var movement = await s.Context.InventoryMovements.SingleAsync();
        Assert.Equal("transfer", movement.Type);
        Assert.Equal(4, movement.Quantity);
        Assert.Equal(s.Warehouse.Id, movement.FacilityId);
        Assert.Equal(userId, movement.UserId);
    }

    [Fact]
    public async Task Confirm_WhenStockDroppedSinceRegistration_IsRejectedAndLeavesStockUntouched()
    {
        var s = await Seed(10);
        var created = await new CreateInventoryTransferCommandHandler(s.Context).Handle(
            new CreateInventoryTransferCommand(s.Product.Id, s.Warehouse.Id, s.Store.Id, 8), CancellationToken.None);
        s.WarehouseStock.AvailableQuantity = 5; // e.g. a sale in the meantime
        await s.Context.SaveChangesAsync(CancellationToken.None);
        var handler = new ConfirmInventoryTransferCommandHandler(s.Context, User);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new ConfirmInventoryTransferCommand(created.Id), CancellationToken.None));

        Assert.Contains("No hay unidades suficientes", ex.Errors["quantity"][0]);
        Assert.Equal(5, (await s.Context.Inventory.SingleAsync()).AvailableQuantity);
        Assert.Equal("pending", (await s.Context.InventoryTransfers.SingleAsync()).Status);
    }

    [Fact]
    public async Task Confirm_AlreadyConfirmed_IsRejectedAndDoesNotDeductTwice()
    {
        var s = await Seed(10);
        var created = await new CreateInventoryTransferCommandHandler(s.Context).Handle(
            new CreateInventoryTransferCommand(s.Product.Id, s.Warehouse.Id, s.Store.Id, 4), CancellationToken.None);
        var handler = new ConfirmInventoryTransferCommandHandler(s.Context, User);
        await handler.Handle(new ConfirmInventoryTransferCommand(created.Id), CancellationToken.None);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new ConfirmInventoryTransferCommand(created.Id), CancellationToken.None));

        Assert.Equal(6, (await s.Context.Inventory.SingleAsync()).AvailableQuantity);
    }

    [Fact]
    public async Task Confirm_UnknownTransfer_ThrowsNotFound()
    {
        var context = InMemoryApplicationDbContext.Create();
        var handler = new ConfirmInventoryTransferCommandHandler(context, User);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new ConfirmInventoryTransferCommand(Guid.NewGuid()), CancellationToken.None));
    }

    // --- Consulta ---

    [Fact]
    public async Task GetTransfers_FiltersByStatusAndListsNewestFirst()
    {
        var s = await Seed(20);
        var create = new CreateInventoryTransferCommandHandler(s.Context);
        var first = await create.Handle(new CreateInventoryTransferCommand(s.Product.Id, s.Warehouse.Id, s.Store.Id, 1), CancellationToken.None);
        var second = await create.Handle(new CreateInventoryTransferCommand(s.Product.Id, s.Warehouse.Id, s.Store.Id, 2), CancellationToken.None);
        (await s.Context.InventoryTransfers.SingleAsync(t => t.Id == first.Id)).RequestedAt = DateTime.UtcNow.AddHours(-1);
        await s.Context.SaveChangesAsync(CancellationToken.None);
        await new ConfirmInventoryTransferCommandHandler(s.Context, User)
            .Handle(new ConfirmInventoryTransferCommand(first.Id), CancellationToken.None);
        var handler = new GetInventoryTransfersQueryHandler(s.Context);

        var all = await handler.Handle(new GetInventoryTransfersQuery(null), CancellationToken.None);
        var pending = await handler.Handle(new GetInventoryTransfersQuery("pending"), CancellationToken.None);

        Assert.Equal([second.Id, first.Id], all.Select(t => t.Id));
        Assert.Equal([second.Id], pending.Select(t => t.Id));
    }

    [Fact]
    public async Task GetFacilities_ReturnsEveryFacilityByName()
    {
        var s = await Seed(1);

        var result = await new GetFacilitiesQueryHandler(s.Context).Handle(new GetFacilitiesQuery(), CancellationToken.None);

        Assert.Equal(["Bodega Central", "Tienda Norte"], result.Select(f => f.Name));
        Assert.Equal(["WAREHOUSE", "POS"], result.Select(f => f.Type));
    }
}

public class ReceiveInventoryTransferHandlerTests
{
    private static readonly FakeCurrentUserService User = new();

    private static async Task<(InMemoryApplicationDbContext Context, Product Product, Facility Warehouse, Facility Store, Guid TransferId)> SeedInTransit(bool confirm = true)
    {
        var context = InMemoryApplicationDbContext.Create();
        var product = new Product { Sku = "SKU-1", Name = "Aspiradora Robot", Price = 899_900m };
        var warehouse = new Facility { Name = "Bodega Central", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" };
        var store = new Facility { Name = "Tienda 1", Type = FacilityType.POS, Address = "Calle 2", City = "Bogotá" };
        context.Products.Add(product);
        context.Facilities.AddRange(warehouse, store);
        context.Inventory.Add(new Inventory { ProductId = product.Id, FacilityId = warehouse.Id, AvailableQuantity = 10 });
        await context.SaveChangesAsync(CancellationToken.None);

        var created = await new CreateInventoryTransferCommandHandler(context).Handle(
            new CreateInventoryTransferCommand(product.Id, warehouse.Id, store.Id, 4), CancellationToken.None);
        if (confirm)
        {
            await new ConfirmInventoryTransferCommandHandler(context, User)
                .Handle(new ConfirmInventoryTransferCommand(created.Id), CancellationToken.None);
        }
        return (context, product, warehouse, store, created.Id);
    }

    [Fact]
    public async Task Receive_CreditsDestination_CreatingTheInventoryRowWhenMissing()
    {
        var (context, product, warehouse, store, id) = await SeedInTransit();

        var result = await new ReceiveInventoryTransferCommandHandler(context, User)
            .Handle(new ReceiveInventoryTransferCommand(id), CancellationToken.None);

        Assert.Equal("completed", result.Status);
        Assert.NotNull(result.CompletedAt);
        var destination = await context.Inventory.SingleAsync(i => i.FacilityId == store.Id && i.ProductId == product.Id);
        Assert.Equal(4, destination.AvailableQuantity);
        Assert.Equal(6, (await context.Inventory.SingleAsync(i => i.FacilityId == warehouse.Id)).AvailableQuantity);
    }

    [Fact]
    public async Task Receive_AddsToExistingDestinationStock_AndLogsMovementAtDestination()
    {
        var (context, product, _, store, id) = await SeedInTransit();
        context.Inventory.Add(new Inventory { ProductId = product.Id, FacilityId = store.Id, AvailableQuantity = 3 });
        await context.SaveChangesAsync(CancellationToken.None);

        await new ReceiveInventoryTransferCommandHandler(context, User)
            .Handle(new ReceiveInventoryTransferCommand(id), CancellationToken.None);

        Assert.Equal(7, (await context.Inventory.SingleAsync(i => i.FacilityId == store.Id)).AvailableQuantity);
        var movement = await context.InventoryMovements.SingleAsync(m => m.FacilityId == store.Id);
        Assert.Equal("transfer", movement.Type);
        Assert.Equal(4, movement.Quantity);
    }

    [Fact]
    public async Task Receive_BeforeConfirming_IsRejected()
    {
        var (context, _, _, store, id) = await SeedInTransit(confirm: false);

        await Assert.ThrowsAsync<ValidationException>(() => new ReceiveInventoryTransferCommandHandler(context, User)
            .Handle(new ReceiveInventoryTransferCommand(id), CancellationToken.None));

        Assert.DoesNotContain(context.Inventory, i => i.FacilityId == store.Id);
    }

    [Fact]
    public async Task Receive_Twice_DoesNotCreditTwice()
    {
        var (context, _, _, store, id) = await SeedInTransit();
        var handler = new ReceiveInventoryTransferCommandHandler(context, User);
        await handler.Handle(new ReceiveInventoryTransferCommand(id), CancellationToken.None);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(new ReceiveInventoryTransferCommand(id), CancellationToken.None));

        Assert.Equal(4, (await context.Inventory.SingleAsync(i => i.FacilityId == store.Id)).AvailableQuantity);
    }

    [Fact]
    public async Task Receive_UnknownTransfer_ThrowsNotFound()
    {
        var context = InMemoryApplicationDbContext.Create();

        await Assert.ThrowsAsync<NotFoundException>(() => new ReceiveInventoryTransferCommandHandler(context, User)
            .Handle(new ReceiveInventoryTransferCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
