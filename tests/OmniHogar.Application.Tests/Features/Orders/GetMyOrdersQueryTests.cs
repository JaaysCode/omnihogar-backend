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
}
