using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.Products;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using NotFoundException = OmniHogar.Domain.Exceptions.NotFoundException;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Tests.Features.Products;

public class StockAlertsTests
{
    private static Facility NewFacility(string name) =>
        new() { Name = name, Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" };

    private static Product NewProduct(string sku, string name) =>
        new() { Sku = sku, Name = name, Price = 100m, Status = "active" };

    private static async Task<(InMemoryApplicationDbContext Context, Facility Facility, Product Product)> Seed(
        int available, int minimum)
    {
        var context = InMemoryApplicationDbContext.Create();
        var facility = NewFacility("Bodega Central");
        var product = NewProduct("SKU-1", "Taladro");
        context.Facilities.Add(facility);
        context.Products.Add(product);
        context.Inventory.Add(new Inventory { ProductId = product.Id, FacilityId = facility.Id, AvailableQuantity = available, MinimumStock = minimum });
        await context.SaveChangesAsync(CancellationToken.None);
        return (context, facility, product);
    }

    private static Task<List<StockAlertDto>> Alerts(InMemoryApplicationDbContext context) =>
        new GetStockAlertsQueryHandler(context).Handle(new GetStockAlertsQuery(), CancellationToken.None);

    // --- HU-24 crit. 1: units equal to or below the minimum generate an alert ---

    [Theory]
    [InlineData(10, 10)] // exactly at the minimum
    [InlineData(3, 10)] // below
    [InlineData(0, 10)] // out of stock
    public async Task UnitsAtOrBelowTheMinimum_GenerateAnAlert(int available, int minimum)
    {
        var (context, _, _) = await Seed(available, minimum);

        Assert.Single(await Alerts(context));
    }

    // --- HU-24 crit. 3: sufficient stock does not alert ---

    [Fact]
    public async Task UnitsAboveTheMinimum_DoNotAlert()
    {
        var (context, _, _) = await Seed(available: 11, minimum: 10);

        Assert.Empty(await Alerts(context));
    }

    [Fact]
    public async Task ProductWithoutAConfiguredMinimum_NeverAlerts_EvenWhenOutOfStock()
    {
        var (context, _, _) = await Seed(available: 0, minimum: 0);

        Assert.Empty(await Alerts(context));
    }

    // --- HU-24 crit. 2: the alert shows product, available quantity and minimum ---

    [Fact]
    public async Task Alert_CarriesProductAvailableQuantityAndConfiguredMinimum()
    {
        var (context, facility, product) = await Seed(available: 4, minimum: 12);

        var alert = Assert.Single(await Alerts(context));

        Assert.Equal(product.Id, alert.ProductId);
        Assert.Equal("SKU-1", alert.Sku);
        Assert.Equal("Taladro", alert.ProductName);
        Assert.Equal(facility.Id, alert.FacilityId);
        Assert.Equal("Bodega Central", alert.FacilityName);
        Assert.Equal(4, alert.AvailableQuantity);
        Assert.Equal(12, alert.MinimumStock);
    }

    [Fact]
    public async Task Alerts_AreOrderedMostUrgentFirst()
    {
        var (context, facility, _) = await Seed(available: 9, minimum: 10);
        var urgent = NewProduct("SKU-2", "Martillo");
        context.Products.Add(urgent);
        context.Inventory.Add(new Inventory { ProductId = urgent.Id, FacilityId = facility.Id, AvailableQuantity = 0, MinimumStock = 10 });
        await context.SaveChangesAsync(CancellationToken.None);

        var alerts = await Alerts(context);

        Assert.Equal(["Martillo", "Taladro"], alerts.Select(a => a.ProductName));
    }

    // --- Configuring the minimum ---

    [Fact]
    public async Task SettingTheMinimum_UpdatesTheExistingInventoryRow_AndStartsAlerting()
    {
        var (context, facility, product) = await Seed(available: 5, minimum: 0);
        Assert.Empty(await Alerts(context));

        await new SetInventoryMinimumStockCommandHandler(context)
            .Handle(new SetInventoryMinimumStockCommand(product.Id, facility.Id, 8), CancellationToken.None);

        Assert.Equal(8, (await context.Inventory.SingleAsync()).MinimumStock);
        Assert.Single(await Alerts(context));
    }

    [Fact]
    public async Task SettingTheMinimum_WhereTheProductHasNoInventoryYet_CreatesTheRowWithZeroUnits()
    {
        var context = InMemoryApplicationDbContext.Create();
        var facility = NewFacility("Bodega Central");
        var product = NewProduct("SKU-1", "Taladro");
        context.Facilities.Add(facility);
        context.Products.Add(product);
        await context.SaveChangesAsync(CancellationToken.None);

        await new SetInventoryMinimumStockCommandHandler(context)
            .Handle(new SetInventoryMinimumStockCommand(product.Id, facility.Id, 5), CancellationToken.None);

        var inventory = await context.Inventory.SingleAsync();
        Assert.Equal(0, inventory.AvailableQuantity);
        Assert.Equal(5, inventory.MinimumStock);
    }

    [Fact]
    public async Task SettingTheMinimum_OfAnUnknownProduct_ThrowsNotFound()
    {
        var (context, facility, _) = await Seed(available: 5, minimum: 0);

        await Assert.ThrowsAsync<NotFoundException>(() => new SetInventoryMinimumStockCommandHandler(context)
            .Handle(new SetInventoryMinimumStockCommand(Guid.NewGuid(), facility.Id, 5), CancellationToken.None));
    }

    [Fact]
    public async Task SettingTheMinimum_OfAnUnknownFacility_IsRejected()
    {
        var (context, _, product) = await Seed(available: 5, minimum: 0);

        await Assert.ThrowsAsync<ValidationException>(() => new SetInventoryMinimumStockCommandHandler(context)
            .Handle(new SetInventoryMinimumStockCommand(product.Id, Guid.NewGuid(), 5), CancellationToken.None));
    }

    [Fact]
    public void ANegativeMinimum_IsRejectedByTheValidator()
    {
        var validator = new SetInventoryMinimumStockCommandValidator();

        validator.TestValidate(new SetInventoryMinimumStockCommand(Guid.NewGuid(), Guid.NewGuid(), -1))
            .ShouldHaveValidationErrorFor(x => x.MinimumStock);
        validator.TestValidate(new SetInventoryMinimumStockCommand(Guid.NewGuid(), Guid.NewGuid(), 0))
            .ShouldNotHaveValidationErrorFor(x => x.MinimumStock);
    }

    [Fact]
    public async Task ProductStockLookup_ExposesTheConfiguredMinimumPerFacility()
    {
        var (context, _, product) = await Seed(available: 5, minimum: 7);

        var stock = await new GetProductStockQueryHandler(context)
            .Handle(new GetProductStockQuery(product.Id), CancellationToken.None);

        Assert.Equal(7, Assert.Single(stock.Facilities).MinimumStock);
    }
}
