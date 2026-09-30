using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.Checkout;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using CartEntity = OmniHogar.Domain.Entities.Cart;

namespace OmniHogar.Application.Tests.Features.Checkout;

public class GetCheckoutStatusQueryHandlerTests
{
    private static User Customer(Guid id) =>
        new() { Id = id, UserType = UserType.customer, Email = $"{id:N}@x.test", FirstName = "C", LastName = "X", PasswordHash = "h" };

    private static async Task<(InMemoryApplicationDbContext Context, FakeCurrentUserService User, FakePaymentGatewayClient Gateway, Order Order, Guid ProductId)> SeedPendingOrder(int available = 10)
    {
        var context = InMemoryApplicationDbContext.Create();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var facility = new Facility { Name = "Bodega", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" };

        context.Users.Add(Customer(userId));
        context.Products.Add(new Product { Id = productId, Sku = "SKU-1", Name = "Silla", Price = 100m, Status = "active" });
        context.Facilities.Add(facility);
        context.Inventory.Add(new Inventory { ProductId = productId, FacilityId = facility.Id, AvailableQuantity = available });

        var cart = new CartEntity { UserId = userId, Channel = "web", Status = "active" };
        context.Carts.Add(cart);

        var order = new Order
        {
            OrderNumber = "WEB-TEST-0001",
            UserId = userId,
            FacilityId = facility.Id,
            Channel = "web",
            Status = "pending_payment",
            Subtotal = 200m,
            Total = 238m,
        };
        order.Items.Add(new OrderItem { ProductId = productId, Quantity = 2, UnitPrice = 100m, Subtotal = 200m });
        context.Orders.Add(order);
        context.Payments.Add(new Payment { OrderId = order.Id, PaymentMethod = "card", Amount = 238m, Status = "pending" });

        await context.SaveChangesAsync(CancellationToken.None);

        return (context, new FakeCurrentUserService { UserId = userId.ToString() }, new FakePaymentGatewayClient(), order, productId);
    }

    [Fact]
    public async Task ApprovedPayment_MarksOrderApprovedDeductsInventoryAndConvertsCart()
    {
        var (context, user, gateway, order, productId) = await SeedPendingOrder(available: 10);
        gateway.PaymentToReturn = new() { Id = "pay-1", Status = "approved", ExternalReference = order.Id.ToString() };
        var handler = new GetCheckoutStatusQueryHandler(context, user, gateway);

        var result = await handler.Handle(new GetCheckoutStatusQuery(order.Id, "pay-1"), CancellationToken.None);

        Assert.Equal("payment_approved", result.OrderStatus);
        Assert.Equal("approved", result.PaymentStatus);
        Assert.False(result.GatewayUnavailable);

        var inventory = await context.Inventory.SingleAsync(i => i.ProductId == productId);
        Assert.Equal(8, inventory.AvailableQuantity);
        Assert.Single(await context.InventoryMovements.ToListAsync());

        var cart = await context.Carts.SingleAsync(c => c.UserId == order.UserId);
        Assert.Equal("converted", cart.Status);
    }

    [Fact]
    public async Task RejectedPayment_MarksOrderRejectedAndLeavesInventoryIntact()
    {
        var (context, user, gateway, order, productId) = await SeedPendingOrder(available: 10);
        gateway.PaymentToReturn = new() { Id = "pay-1", Status = "rejected", ExternalReference = order.Id.ToString() };
        var handler = new GetCheckoutStatusQueryHandler(context, user, gateway);

        var result = await handler.Handle(new GetCheckoutStatusQuery(order.Id, "pay-1"), CancellationToken.None);

        Assert.Equal("payment_rejected", result.OrderStatus);
        Assert.Equal("rejected", result.PaymentStatus);

        var inventory = await context.Inventory.SingleAsync(i => i.ProductId == productId);
        Assert.Equal(10, inventory.AvailableQuantity);
    }

    [Fact]
    public async Task StillPendingAtTheGateway_LeavesEverythingUnchanged()
    {
        var (context, user, gateway, order, _) = await SeedPendingOrder();
        gateway.PaymentToReturn = new() { Id = "pay-1", Status = "in_process", ExternalReference = order.Id.ToString() };
        var handler = new GetCheckoutStatusQueryHandler(context, user, gateway);

        var result = await handler.Handle(new GetCheckoutStatusQuery(order.Id, "pay-1"), CancellationToken.None);

        Assert.Equal("pending_payment", result.OrderStatus);
        Assert.Equal("pending", result.PaymentStatus);
        Assert.False(result.GatewayUnavailable);
    }

    [Fact]
    public async Task GatewayCommunicationError_ReportsGatewayUnavailableWithoutThrowing()
    {
        var (context, user, gateway, order, _) = await SeedPendingOrder();
        gateway.ThrowOnGetPayment = true;
        var handler = new GetCheckoutStatusQueryHandler(context, user, gateway);

        var result = await handler.Handle(new GetCheckoutStatusQuery(order.Id, "pay-1"), CancellationToken.None);

        Assert.True(result.GatewayUnavailable);
        Assert.Equal("pending_payment", result.OrderStatus);
    }

    [Fact]
    public async Task NoPaymentIdAndNoPaymentYet_StaysPending()
    {
        var (context, user, gateway, order, _) = await SeedPendingOrder();
        var handler = new GetCheckoutStatusQueryHandler(context, user, gateway);

        var result = await handler.Handle(new GetCheckoutStatusQuery(order.Id, null), CancellationToken.None);

        Assert.Equal("pending_payment", result.OrderStatus);
        Assert.Null(gateway.LastPaymentIdRequested);
        Assert.Equal(order.Id.ToString(), gateway.LastExternalReferenceSearched);
    }

    [Fact]
    public async Task NoPaymentIdGiven_ConfirmsViaExternalReferenceSearch()
    {
        var (context, user, gateway, order, _) = await SeedPendingOrder();
        gateway.LatestPaymentToReturn = new() { Id = "pay-9", Status = "approved", ExternalReference = order.Id.ToString() };
        var handler = new GetCheckoutStatusQueryHandler(context, user, gateway);

        var result = await handler.Handle(new GetCheckoutStatusQuery(order.Id, null), CancellationToken.None);

        Assert.Equal("payment_approved", result.OrderStatus);
        Assert.Equal("approved", result.PaymentStatus);
    }

    [Fact]
    public async Task PaymentIdFromAnotherOrder_IsIgnored()
    {
        var (context, user, gateway, order, _) = await SeedPendingOrder();
        gateway.PaymentToReturn = new() { Id = "pay-1", Status = "approved", ExternalReference = Guid.NewGuid().ToString() };
        var handler = new GetCheckoutStatusQueryHandler(context, user, gateway);

        var result = await handler.Handle(new GetCheckoutStatusQuery(order.Id, "pay-1"), CancellationToken.None);

        Assert.Equal("pending_payment", result.OrderStatus);
    }

    [Fact]
    public async Task Cancelled_MarksPaymentRejectedWithoutCallingTheGateway()
    {
        var (context, user, gateway, order, _) = await SeedPendingOrder();
        var handler = new GetCheckoutStatusQueryHandler(context, user, gateway);

        var result = await handler.Handle(new GetCheckoutStatusQuery(order.Id, null, Cancelled: true), CancellationToken.None);

        Assert.Equal("payment_rejected", result.OrderStatus);
        Assert.Equal("rejected", result.PaymentStatus);
        Assert.False(result.GatewayUnavailable);
        Assert.Null(gateway.LastPaymentIdRequested);
        Assert.Null(gateway.LastExternalReferenceSearched);
    }

    [Fact]
    public async Task Cancelled_OnAnAlreadyApprovedPayment_LeavesItApproved()
    {
        var (context, user, gateway, order, _) = await SeedPendingOrder();
        gateway.PaymentToReturn = new() { Id = "pay-1", Status = "approved", ExternalReference = order.Id.ToString() };
        var handler = new GetCheckoutStatusQueryHandler(context, user, gateway);

        // First confirm it normally (as if the buyer's redirect raced the "cancelled" one)...
        await handler.Handle(new GetCheckoutStatusQuery(order.Id, "pay-1"), CancellationToken.None);
        // ...then a stray cancelled check comes in — idempotency guard must ignore it.
        var result = await handler.Handle(new GetCheckoutStatusQuery(order.Id, null, Cancelled: true), CancellationToken.None);

        Assert.Equal("payment_approved", result.OrderStatus);
        Assert.Equal("approved", result.PaymentStatus);
    }
}
