using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.Checkout;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using CartEntity = OmniHogar.Domain.Entities.Cart;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Tests.Features.Checkout;

public class CreateCheckoutPreferenceCommandHandlerTests
{
    private static User Customer(Guid id) =>
        new() { Id = id, UserType = UserType.customer, Email = $"{id:N}@x.test", FirstName = "C", LastName = "X", PasswordHash = "h" };

    private static CustomerAddressInput ValidAddress() => new("Calle 123 #45-67", "Bogotá", "Chapinero", "Casa azul");

    private static async Task<(InMemoryApplicationDbContext Context, FakeCurrentUserService User, FakePaymentGatewayClient Gateway, Guid ProductId)> SeedCartWithLine(
        int available, int quantity = 2, decimal price = 100m)
    {
        var context = InMemoryApplicationDbContext.Create();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var facility = new Facility { Name = "Bodega", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" };

        context.Users.Add(Customer(userId));
        context.Products.Add(new Product { Id = productId, Sku = "SKU-1", Name = "Silla", Price = price, Status = "active" });
        context.Facilities.Add(facility);
        context.Inventory.Add(new Inventory { ProductId = productId, FacilityId = facility.Id, AvailableQuantity = available });
        context.Carts.Add(new CartEntity
        {
            UserId = userId,
            Channel = "web",
            Status = "active",
            Items = { new CartItem { ProductId = productId, Quantity = quantity, UnitPrice = price } },
        });
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, new FakeCurrentUserService { UserId = userId.ToString(), Email = "cliente@x.test" }, new FakePaymentGatewayClient(), productId);
    }

    [Fact]
    public async Task SuccessfulCheckout_CreatesAddressOrderAndPendingPayment()
    {
        var (context, user, gateway, productId) = await SeedCartWithLine(available: 10, quantity: 2, price: 100m);
        var handler = new CreateCheckoutPreferenceCommandHandler(context, user, gateway, new FakePaymentGatewayUrls());

        var result = await handler.Handle(new CreateCheckoutPreferenceCommand(ValidAddress(), "card"), CancellationToken.None);

        var order = await context.Orders.Include(o => o.Items).SingleAsync();
        Assert.Equal("web", order.Channel);
        Assert.Equal("pending_payment", order.Status);
        Assert.Equal(200m, order.Subtotal);
        Assert.Equal(238m, order.Total); // 200 * 1.19
        Assert.Single(order.Items);

        var address = await context.CustomerAddresses.SingleAsync();
        Assert.Equal(order.ShippingAddressId, address.Id);
        Assert.Equal("Calle 123 #45-67", address.Address);

        var payment = await context.Payments.SingleAsync();
        Assert.Equal("pending", payment.Status);
        Assert.Equal("card", payment.PaymentMethod);
        Assert.Equal(order.Total, payment.Amount);

        Assert.Equal(order.Id, result.OrderId);
        Assert.Equal(gateway.PreferenceToReturn.InitPoint, result.InitPoint);
        Assert.Equal(productId, order.Items.Single().ProductId);
    }

    [Fact]
    public async Task EmptyCart_ThrowsValidationExceptionAndCreatesNothing()
    {
        var context = InMemoryApplicationDbContext.Create();
        var userId = Guid.NewGuid();
        context.Users.Add(Customer(userId));
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateCheckoutPreferenceCommandHandler(
            context, new FakeCurrentUserService { UserId = userId.ToString() }, new FakePaymentGatewayClient(), new FakePaymentGatewayUrls());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new CreateCheckoutPreferenceCommand(ValidAddress(), "card"), CancellationToken.None));

        Assert.Empty(context.Orders);
    }

    [Fact]
    public async Task QuantityBeyondAvailable_ThrowsValidationExceptionAndCreatesNothing()
    {
        var (context, user, gateway, _) = await SeedCartWithLine(available: 1, quantity: 5);
        var handler = new CreateCheckoutPreferenceCommandHandler(context, user, gateway, new FakePaymentGatewayUrls());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new CreateCheckoutPreferenceCommand(ValidAddress(), "card"), CancellationToken.None));

        Assert.Empty(context.Orders);
        Assert.Empty(context.Payments);
    }

    [Fact]
    public async Task GatewayCommunicationError_KeepsTheOrderAndPaymentButThrows()
    {
        var (context, user, gateway, _) = await SeedCartWithLine(available: 10);
        gateway.ThrowOnCreatePreference = true;
        var handler = new CreateCheckoutPreferenceCommandHandler(context, user, gateway, new FakePaymentGatewayUrls());

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new CreateCheckoutPreferenceCommand(ValidAddress(), "card"), CancellationToken.None));

        Assert.Contains("No fue posible conectar", ex.Errors["Gateway"][0]);
        Assert.Single(await context.Orders.ToListAsync());
        Assert.Single(await context.Payments.ToListAsync());
    }
}
