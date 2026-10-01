using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OmniHogar.Application.Common.Mappings;
using OmniHogar.Application.Features.Orders;
using OmniHogar.Application.Tests.TestSupport;
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
}
