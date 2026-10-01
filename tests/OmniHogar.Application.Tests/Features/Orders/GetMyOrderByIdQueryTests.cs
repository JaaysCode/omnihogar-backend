using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using OmniHogar.Application.Common.Mappings;
using OmniHogar.Application.Features.Orders;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Tests.Features.Orders;

public class GetMyOrderByIdQueryTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task Owner_CanReadTheirOwnOrder()
    {
        var context = InMemoryApplicationDbContext.Create();
        var me = new User { UserType = UserType.customer, FirstName = "A", LastName = "A", Email = "a@example.com", PasswordHash = "h" };
        context.Users.Add(me);
        var order = new Order { OrderNumber = "ORD-1", UserId = me.Id, User = me, Channel = "web", Status = "preparing" };
        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyOrderByIdQueryHandler(context, CreateMapper(), new FakeCurrentUserService { UserId = me.Id.ToString() });
        var result = await handler.Handle(new GetMyOrderByIdQuery(order.Id), CancellationToken.None);

        Assert.Equal("ORD-1", result.OrderNumber);
    }

    [Fact]
    public async Task NonOwner_GetsNotFoundException()
    {
        var context = InMemoryApplicationDbContext.Create();
        var owner = new User { UserType = UserType.customer, FirstName = "A", LastName = "A", Email = "a@example.com", PasswordHash = "h" };
        var someoneElse = new User { UserType = UserType.customer, FirstName = "B", LastName = "B", Email = "b@example.com", PasswordHash = "h" };
        context.Users.AddRange(owner, someoneElse);
        var order = new Order { OrderNumber = "ORD-1", UserId = owner.Id, User = owner, Channel = "web", Status = "preparing" };
        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyOrderByIdQueryHandler(context, CreateMapper(), new FakeCurrentUserService { UserId = someoneElse.Id.ToString() });

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetMyOrderByIdQuery(order.Id), CancellationToken.None));
    }
}
