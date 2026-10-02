using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OmniHogar.Application.Common.Mappings;
using OmniHogar.Application.Features.Orders;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Constants;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Tests.Features.Orders;

public class UpdateOrderStatusCommandHandlerTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static async Task<(InMemoryApplicationDbContext Context, Order Order, Guid DispatcherId)> Seed(string initialStatus)
    {
        var context = InMemoryApplicationDbContext.Create();
        var customer = new User { UserType = UserType.customer, FirstName = "Camila", LastName = "Ruiz", Email = "camila@example.com", PasswordHash = "h" };
        var dispatcherId = Guid.NewGuid();
        context.Users.Add(customer);

        var order = new Order
        {
            OrderNumber = "ORD-900",
            UserId = customer.Id,
            User = customer,
            Channel = "web",
            Status = initialStatus,
            Subtotal = 100_000m,
            Total = 100_000m,
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, order, dispatcherId);
    }

    [Fact]
    public async Task ValidTransition_UpdatesOrderStatusAndReturnsUpdatedDetail()
    {
        var (context, order, dispatcherId) = await Seed("preparing");
        var handler = new UpdateOrderStatusCommandHandler(context, CreateMapper(), new FakeCurrentUserService { UserId = dispatcherId.ToString() });

        var result = await handler.Handle(new UpdateOrderStatusCommand(order.Id, "packed", null), CancellationToken.None);

        Assert.Equal("packed", result.Status);
        var persisted = await context.Orders.SingleAsync(o => o.Id == order.Id);
        Assert.Equal("packed", persisted.Status);
    }

    [Fact]
    public async Task ValidTransition_WritesOrderStatusHistoryRowWithPreviousAndNewStatus()
    {
        var (context, order, dispatcherId) = await Seed("preparing");
        var handler = new UpdateOrderStatusCommandHandler(context, CreateMapper(), new FakeCurrentUserService { UserId = dispatcherId.ToString() });

        await handler.Handle(new UpdateOrderStatusCommand(order.Id, "shipped", "Salió con la ruta 3"), CancellationToken.None);

        var history = await context.OrderStatusHistories.SingleAsync(h => h.OrderId == order.Id);
        Assert.Equal("preparing", history.PreviousStatus);
        Assert.Equal("shipped", history.NewStatus);
        Assert.Equal(dispatcherId, history.UserId);
        Assert.Equal("Salió con la ruta 3", history.Comment);
    }

    [Fact]
    public async Task NonexistentOrder_ThrowsNotFoundException()
    {
        var context = InMemoryApplicationDbContext.Create();
        var handler = new UpdateOrderStatusCommandHandler(context, CreateMapper(), new FakeCurrentUserService());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new UpdateOrderStatusCommand(Guid.NewGuid(), "packed", null), CancellationToken.None));
    }

    // --- HU-13: entering "preparing" opens the Dispatch and notifies the despacho team ---

    private static async Task<Guid> AddDespachoUserAsync(InMemoryApplicationDbContext context, bool active = true)
    {
        var user = new User
        {
            UserType = UserType.employee,
            FirstName = "Diego",
            LastName = "Coordinador",
            Email = $"{Guid.NewGuid()}@example.com",
            PasswordHash = "h",
            Status = active,
        };
        context.Users.Add(user);
        context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = SeededRoleIds.CoordinadorDeDespacho });
        await context.SaveChangesAsync(CancellationToken.None);
        return user.Id;
    }

    [Fact]
    public async Task TransitionToPreparing_OpensDispatchAndNotifiesDespachoTeam()
    {
        var (context, order, dispatcherId) = await Seed("payment_approved");
        var despachoUserId = await AddDespachoUserAsync(context);
        var handler = new UpdateOrderStatusCommandHandler(context, CreateMapper(), new FakeCurrentUserService { UserId = dispatcherId.ToString() });

        var result = await handler.Handle(new UpdateOrderStatusCommand(order.Id, "preparing", null), CancellationToken.None);

        Assert.True(result.DispatchNotified);

        var dispatch = await context.Dispatches.SingleAsync(d => d.OrderId == order.Id);
        Assert.NotNull(dispatch.NotifiedAt);

        var notification = await context.Notifications.SingleAsync(n => n.UserId == despachoUserId);
        Assert.Equal("dispatch_ready", notification.Type);
        Assert.Equal("in_app", notification.Channel);
        Assert.Equal(order.Id, notification.OrderId);
        Assert.Contains(order.OrderNumber, notification.Content);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task TransitionToPreparing_NoDespachoUsers_MarksNotNotifiedButKeepsOrderInPreparing()
    {
        var (context, order, dispatcherId) = await Seed("payment_approved");
        var handler = new UpdateOrderStatusCommandHandler(context, CreateMapper(), new FakeCurrentUserService { UserId = dispatcherId.ToString() });

        var result = await handler.Handle(new UpdateOrderStatusCommand(order.Id, "preparing", null), CancellationToken.None);

        // crit. 3: the order is NOT reverted/blocked even though notification couldn't be sent.
        Assert.False(result.DispatchNotified);
        Assert.Equal("preparing", result.Status);

        var dispatch = await context.Dispatches.SingleAsync(d => d.OrderId == order.Id);
        Assert.Null(dispatch.NotifiedAt);
        Assert.Empty(await context.Notifications.ToListAsync());
    }

    [Fact]
    public async Task TransitionNotIntoPreparing_LeavesDispatchNotifiedNull()
    {
        var (context, order, dispatcherId) = await Seed("preparing");
        var handler = new UpdateOrderStatusCommandHandler(context, CreateMapper(), new FakeCurrentUserService { UserId = dispatcherId.ToString() });

        var result = await handler.Handle(new UpdateOrderStatusCommand(order.Id, "packed", null), CancellationToken.None);

        Assert.Null(result.DispatchNotified);
    }
}
