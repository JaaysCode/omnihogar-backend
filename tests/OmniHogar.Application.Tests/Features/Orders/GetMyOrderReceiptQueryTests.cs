using OmniHogar.Application.Features.Orders;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using NotFoundException = OmniHogar.Domain.Exceptions.NotFoundException;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Tests.Features.Orders;

public class GetMyOrderReceiptQueryTests
{
    private static async Task<(InMemoryApplicationDbContext Context, User Me, Order Order)> Seed(
        params Payment[] payments)
    {
        var context = InMemoryApplicationDbContext.Create();
        var me = new User { UserType = UserType.customer, FirstName = "Ana", LastName = "Gómez", Email = "ana@example.com", PasswordHash = "h" };
        var product = new Product { Sku = "SKU-1", Name = "Silla", Price = 100_000m, Status = "active" };
        context.Users.Add(me);
        context.Products.Add(product);

        var order = new Order
        {
            OrderNumber = "WEB-20261020-101010-1234",
            UserId = me.Id,
            User = me,
            Channel = "web",
            Status = "payment_approved",
            Subtotal = 200_000m,
            Total = 200_000m,
        };
        order.Items.Add(new OrderItem { Order = order, ProductId = product.Id, Product = product, Quantity = 2, UnitPrice = 100_000m, Subtotal = 200_000m });
        foreach (var payment in payments)
        {
            payment.OrderId = order.Id;
            order.Payments.Add(payment);
        }

        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);
        return (context, me, order);
    }

    private static GetMyOrderReceiptQueryHandler HandlerFor(InMemoryApplicationDbContext context, User user) =>
        new(context, new FakeCurrentUserService { UserId = user.Id.ToString() });

    private static Payment Approved(DateTime? confirmedAt = null, DateTime? createdAt = null) => new()
    {
        PaymentMethod = "card",
        Amount = 200_000m,
        Status = "approved",
        ConfirmedAt = confirmedAt ?? new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc),
        CreatedAt = createdAt ?? new DateTime(2026, 10, 20, 14, 55, 0, DateTimeKind.Utc),
    };

    [Fact]
    public async Task ApprovedPayment_ReturnsReceiptWithOrderNumberDateAmountAndMethod()
    {
        var (context, me, order) = await Seed(Approved());

        var receipt = await HandlerFor(context, me).Handle(new GetMyOrderReceiptQuery(order.Id), CancellationToken.None);

        Assert.Equal("CP-WEB-20261020-101010-1234", receipt.ReceiptNumber);
        Assert.Equal("WEB-20261020-101010-1234", receipt.OrderNumber);
        Assert.Equal(new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc), receipt.PaidAt);
        Assert.Equal(200_000m, receipt.Amount);
        Assert.Equal("card", receipt.PaymentMethod);
        Assert.Equal("Ana Gómez", receipt.CustomerName);
        var line = Assert.Single(receipt.Items);
        Assert.Equal("Silla", line.ProductName);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(200_000m, receipt.Total);
    }

    [Theory]
    [InlineData("pending", "Pendiente")]
    [InlineData("rejected", "Rechazado")]
    [InlineData("reversed", "Reversado")]
    public async Task PaymentNotApproved_HasNoReceiptAndReportsTheTransactionStatus(string status, string label)
    {
        var (context, me, order) = await Seed(new Payment { PaymentMethod = "pse", Amount = 200_000m, Status = status });

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => HandlerFor(context, me).Handle(new GetMyOrderReceiptQuery(order.Id), CancellationToken.None));

        Assert.Contains("no fue aprobado", ex.Errors["orderId"][0]);
        Assert.Contains(label, ex.Errors["orderId"][0]);
    }

    [Fact]
    public async Task OrderWithoutAnOnlinePayment_HasNoReceipt()
    {
        var (context, me, order) = await Seed();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => HandlerFor(context, me).Handle(new GetMyOrderReceiptQuery(order.Id), CancellationToken.None));

        Assert.Contains("no tiene un pago", ex.Errors["orderId"][0]);
    }

    [Fact]
    public async Task AnotherCustomersOrder_IsNotFound()
    {
        var (context, _, order) = await Seed(Approved());
        var stranger = new User { UserType = UserType.customer, FirstName = "Otro", LastName = "Cliente", Email = "otro@example.com", PasswordHash = "h" };
        context.Users.Add(stranger);
        await context.SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(
            () => HandlerFor(context, stranger).Handle(new GetMyOrderReceiptQuery(order.Id), CancellationToken.None));
    }

    [Fact]
    public async Task WithSeveralPayments_UsesTheMostRecentOne()
    {
        var rejected = new Payment
        {
            PaymentMethod = "pse",
            Amount = 200_000m,
            Status = "rejected",
            CreatedAt = new DateTime(2026, 10, 20, 14, 0, 0, DateTimeKind.Utc),
        };
        var (context, me, order) = await Seed(rejected, Approved());

        var receipt = await HandlerFor(context, me).Handle(new GetMyOrderReceiptQuery(order.Id), CancellationToken.None);

        Assert.Equal("card", receipt.PaymentMethod);
    }
}
