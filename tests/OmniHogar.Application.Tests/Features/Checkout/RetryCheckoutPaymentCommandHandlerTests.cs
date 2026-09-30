using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.Checkout;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Tests.Features.Checkout;

public class RetryCheckoutPaymentCommandHandlerTests
{
    private static User Customer(Guid id) =>
        new() { Id = id, UserType = UserType.customer, Email = $"{id:N}@x.test", FirstName = "C", LastName = "X", PasswordHash = "h" };

    private static async Task<(InMemoryApplicationDbContext Context, FakeCurrentUserService User, FakePaymentGatewayClient Gateway, Order Order)> SeedRejectedOrder()
    {
        var context = InMemoryApplicationDbContext.Create();
        var userId = Guid.NewGuid();
        context.Users.Add(Customer(userId));

        var order = new Order
        {
            OrderNumber = "WEB-TEST-0002",
            UserId = userId,
            Channel = "web",
            Status = "payment_rejected",
            Subtotal = 100m,
            Total = 119m,
        };
        context.Orders.Add(order);
        context.Payments.Add(new Payment { OrderId = order.Id, PaymentMethod = "card", Amount = 119m, Status = "rejected" });
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, new FakeCurrentUserService { UserId = userId.ToString() }, new FakePaymentGatewayClient(), order);
    }

    [Fact]
    public async Task RejectedOrder_ResetsToPendingAndReturnsANewInitPoint()
    {
        var (context, user, gateway, order) = await SeedRejectedOrder();
        gateway.PreferenceToReturn = new() { Id = "pref-2", InitPoint = "https://checkout.stripe.com/c/pay/cs_test_pref-2" };
        var handler = new RetryCheckoutPaymentCommandHandler(context, user, gateway, new FakePaymentGatewayUrls());

        var result = await handler.Handle(new RetryCheckoutPaymentCommand(order.Id), CancellationToken.None);

        Assert.Equal("https://checkout.stripe.com/c/pay/cs_test_pref-2", result.InitPoint);

        var refreshedOrder = await context.Orders.SingleAsync();
        Assert.Equal("pending_payment", refreshedOrder.Status);
        var payment = await context.Payments.SingleAsync();
        Assert.Equal("pending", payment.Status);
    }

    [Fact]
    public async Task AlreadyApprovedOrder_ThrowsValidationException()
    {
        var (context, user, gateway, order) = await SeedRejectedOrder();
        order.Status = "payment_approved";
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = new RetryCheckoutPaymentCommandHandler(context, user, gateway, new FakePaymentGatewayUrls());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new RetryCheckoutPaymentCommand(order.Id), CancellationToken.None));
    }

    [Fact]
    public async Task UnknownOrder_ThrowsNotFound()
    {
        var (context, user, gateway, _) = await SeedRejectedOrder();
        var handler = new RetryCheckoutPaymentCommandHandler(context, user, gateway, new FakePaymentGatewayUrls());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new RetryCheckoutPaymentCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task GatewayCommunicationError_KeepsOrderPendingButThrows()
    {
        var (context, user, gateway, order) = await SeedRejectedOrder();
        gateway.ThrowOnCreatePreference = true;
        var handler = new RetryCheckoutPaymentCommandHandler(context, user, gateway, new FakePaymentGatewayUrls());

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new RetryCheckoutPaymentCommand(order.Id), CancellationToken.None));

        Assert.Contains("No fue posible conectar", ex.Errors["Gateway"][0]);
        var refreshedOrder = await context.Orders.SingleAsync();
        Assert.Equal("pending_payment", refreshedOrder.Status);
    }
}
