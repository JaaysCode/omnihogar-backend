using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using OmniHogar.Application.Common.Mappings;
using OmniHogar.Application.Features.Notifications;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Notifications;

public class GetMyNotificationsQueryTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static async Task<(InMemoryApplicationDbContext Context, User User)> SeedUser()
    {
        var context = InMemoryApplicationDbContext.Create();
        var user = new User { UserType = UserType.employee, FirstName = "Diego", LastName = "Coordinador", Email = "diego@example.com", PasswordHash = "h" };
        context.Users.Add(user);
        await context.SaveChangesAsync(CancellationToken.None);
        return (context, user);
    }

    [Fact]
    public async Task ReturnsOnlyTheCurrentUsersInAppNotifications_NewestFirst()
    {
        var (context, user) = await SeedUser();
        var other = new User { UserType = UserType.employee, FirstName = "Otra", LastName = "Persona", Email = "otra@example.com", PasswordHash = "h" };
        context.Users.Add(other);

        var order = new Order { OrderNumber = "ORD-700", UserId = user.Id, Channel = "web", Status = "preparing" };
        context.Orders.Add(order);

        context.Notifications.Add(new Notification
        {
            UserId = user.Id,
            OrderId = order.Id,
            Type = "dispatch_ready",
            Channel = "in_app",
            Content = "Pedido ORD-700 listo para despacho.",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
        });
        var latest = new Notification
        {
            UserId = user.Id,
            OrderId = order.Id,
            Type = "dispatch_ready",
            Channel = "in_app",
            Content = "Pedido ORD-700 listo para despacho (2).",
            CreatedAt = DateTime.UtcNow,
        };
        context.Notifications.Add(latest);
        context.Notifications.Add(new Notification
        {
            UserId = other.Id,
            Type = "dispatch_ready",
            Channel = "in_app",
            Content = "No debería salir.",
        });
        context.Notifications.Add(new Notification
        {
            UserId = user.Id,
            Type = "order_confirmed",
            Channel = "email",
            Content = "Correo, no bandeja in-app.",
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyNotificationsQueryHandler(context, new FakeCurrentUserService { UserId = user.Id.ToString() }, CreateMapper());
        var result = await handler.Handle(new GetMyNotificationsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(latest.Id, result[0].Id);
        Assert.Equal("ORD-700", result[0].OrderNumber);
        Assert.False(result[0].IsRead);
    }
}
