using OmniHogar.Application.Features.Orders;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Tests.Features.Orders;

public class GetMyOrderTrackingQueryTests
{
    private static User NewCustomer(string email) =>
        new() { UserType = UserType.customer, FirstName = "A", LastName = "A", Email = email, PasswordHash = "h" };

    [Fact]
    public async Task Owner_SeesCurrentStatus()
    {
        var context = InMemoryApplicationDbContext.Create();
        var me = NewCustomer("a@example.com");
        context.Users.Add(me);
        var order = new Order { OrderNumber = "ORD-1", UserId = me.Id, User = me, Channel = "web", Status = "preparing" };
        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyOrderTrackingQueryHandler(context, new FakeCurrentUserService { UserId = me.Id.ToString() });
        var result = await handler.Handle(new GetMyOrderTrackingQuery(order.Id), CancellationToken.None);

        Assert.Equal("ORD-1", result.OrderNumber);
        Assert.Equal("preparing", result.Status);
        Assert.Empty(result.History);
        Assert.Equal(order.CreatedAt, result.LastUpdatedAt);
    }

    [Fact]
    public async Task StatusChange_ShowsUpdatedStatusOnNextQuery()
    {
        var context = InMemoryApplicationDbContext.Create();
        var me = NewCustomer("a@example.com");
        context.Users.Add(me);
        var order = new Order { OrderNumber = "ORD-1", UserId = me.Id, User = me, Channel = "web", Status = "preparing" };
        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyOrderTrackingQueryHandler(context, new FakeCurrentUserService { UserId = me.Id.ToString() });
        var before = await handler.Handle(new GetMyOrderTrackingQuery(order.Id), CancellationToken.None);
        Assert.Equal("preparing", before.Status);

        order.Status = "packed";
        context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            PreviousStatus = "preparing",
            NewStatus = "packed",
            Comment = "nota interna",
            CreatedAt = DateTime.UtcNow.AddMinutes(5),
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var after = await handler.Handle(new GetMyOrderTrackingQuery(order.Id), CancellationToken.None);

        Assert.Equal("packed", after.Status);
        var step = Assert.Single(after.History);
        Assert.Equal("packed", step.Status);
        Assert.Equal(step.OccurredAt, after.LastUpdatedAt);
    }

    [Fact]
    public async Task History_IsReturnedOldestFirst()
    {
        var context = InMemoryApplicationDbContext.Create();
        var me = NewCustomer("a@example.com");
        context.Users.Add(me);
        var order = new Order { OrderNumber = "ORD-1", UserId = me.Id, User = me, Channel = "web", Status = "shipped" };
        context.Orders.Add(order);
        var t0 = DateTime.UtcNow;
        context.OrderStatusHistories.AddRange(
            new OrderStatusHistory { OrderId = order.Id, NewStatus = "shipped", CreatedAt = t0.AddHours(2) },
            new OrderStatusHistory { OrderId = order.Id, NewStatus = "preparing", CreatedAt = t0.AddHours(1) });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyOrderTrackingQueryHandler(context, new FakeCurrentUserService { UserId = me.Id.ToString() });
        var result = await handler.Handle(new GetMyOrderTrackingQuery(order.Id), CancellationToken.None);

        Assert.Equal(["preparing", "shipped"], result.History.Select(h => h.Status));
    }

    [Fact]
    public async Task NonOwner_GetsNotFoundWithPedidoNoEncontradoMessage()
    {
        var context = InMemoryApplicationDbContext.Create();
        var owner = NewCustomer("a@example.com");
        var other = NewCustomer("b@example.com");
        context.Users.AddRange(owner, other);
        var order = new Order { OrderNumber = "ORD-1", UserId = owner.Id, User = owner, Channel = "web", Status = "preparing" };
        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyOrderTrackingQueryHandler(context, new FakeCurrentUserService { UserId = other.Id.ToString() });

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetMyOrderTrackingQuery(order.Id), CancellationToken.None));
        Assert.Equal("El pedido no fue encontrado.", ex.Message);
    }

    [Fact]
    public async Task UnknownId_GetsNotFoundException()
    {
        var context = InMemoryApplicationDbContext.Create();
        var handler = new GetMyOrderTrackingQueryHandler(context, new FakeCurrentUserService());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetMyOrderTrackingQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
