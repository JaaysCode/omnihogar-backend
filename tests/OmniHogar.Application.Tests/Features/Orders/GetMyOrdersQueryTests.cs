using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using OmniHogar.Application.Common.Mappings;
using OmniHogar.Application.Features.Orders;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Orders;

public class GetMyOrdersQueryTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task OnlyReturnsOrdersBelongingToTheCurrentUser()
    {
        var context = InMemoryApplicationDbContext.Create();
        var me = new User { UserType = UserType.customer, FirstName = "A", LastName = "A", Email = "a@example.com", PasswordHash = "h" };
        var other = new User { UserType = UserType.customer, FirstName = "B", LastName = "B", Email = "b@example.com", PasswordHash = "h" };
        context.Users.AddRange(me, other);
        context.Orders.Add(new Order { OrderNumber = "ORD-1", UserId = me.Id, User = me, Channel = "web", Status = "preparing" });
        context.Orders.Add(new Order { OrderNumber = "ORD-2", UserId = other.Id, User = other, Channel = "web", Status = "preparing" });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyOrdersQueryHandler(context, CreateMapper(), new FakeCurrentUserService { UserId = me.Id.ToString() });
        var result = await handler.Handle(new GetMyOrdersQuery(), CancellationToken.None);

        var order = Assert.Single(result);
        Assert.Equal("ORD-1", order.OrderNumber);
    }

    private static User Customer(string email) =>
        new() { UserType = UserType.customer, FirstName = "Ana", LastName = "Gómez", Email = email, PasswordHash = "h" };

    [Fact]
    public async Task CustomerWithNoOrders_GetsAnEmptyList()
    {
        var context = InMemoryApplicationDbContext.Create();
        var me = Customer("me@example.com");
        var other = Customer("other@example.com");
        context.Users.AddRange(me, other);
        context.Orders.Add(new Order { OrderNumber = "ORD-9", UserId = other.Id, User = other, Channel = "web", Status = "delivered" });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyOrdersQueryHandler(context, CreateMapper(), new FakeCurrentUserService { UserId = me.Id.ToString() });
        var result = await handler.Handle(new GetMyOrdersQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task OrdersAreReturnedNewestFirst()
    {
        var context = InMemoryApplicationDbContext.Create();
        var me = Customer("me@example.com");
        context.Users.Add(me);
        context.Orders.Add(new Order { OrderNumber = "ORD-OLD", UserId = me.Id, User = me, Channel = "web", Status = "delivered", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        context.Orders.Add(new Order { OrderNumber = "ORD-NEW", UserId = me.Id, User = me, Channel = "web", Status = "preparing", CreatedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc) });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyOrdersQueryHandler(context, CreateMapper(), new FakeCurrentUserService { UserId = me.Id.ToString() });
        var result = await handler.Handle(new GetMyOrdersQuery(), CancellationToken.None);

        Assert.Equal(["ORD-NEW", "ORD-OLD"], result.Select(o => o.OrderNumber));
    }

    [Fact]
    public async Task EachOrderCarriesIdentifierDateValueAndStatus()
    {
        var context = InMemoryApplicationDbContext.Create();
        var me = Customer("me@example.com");
        context.Users.Add(me);
        var createdAt = new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc);
        var order = new Order { OrderNumber = "ORD-77", UserId = me.Id, User = me, Channel = "web", Status = "shipped", Total = 150_000m, CreatedAt = createdAt };
        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyOrdersQueryHandler(context, CreateMapper(), new FakeCurrentUserService { UserId = me.Id.ToString() });
        var dto = Assert.Single(await handler.Handle(new GetMyOrdersQuery(), CancellationToken.None));

        Assert.Equal(order.Id, dto.Id);
        Assert.Equal("ORD-77", dto.OrderNumber);
        Assert.Equal(createdAt, dto.CreatedAt);
        Assert.Equal(150_000m, dto.Total);
        Assert.Equal("shipped", dto.Status);
    }
}
