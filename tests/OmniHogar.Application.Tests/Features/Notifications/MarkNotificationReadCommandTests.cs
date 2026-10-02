using OmniHogar.Application.Features.Notifications;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Tests.Features.Notifications;

public class MarkNotificationReadCommandTests
{
    private static async Task<(InMemoryApplicationDbContext Context, User User, Notification Notification)> Seed()
    {
        var context = InMemoryApplicationDbContext.Create();
        var user = new User { UserType = UserType.employee, FirstName = "Diego", LastName = "Coordinador", Email = "diego@example.com", PasswordHash = "h" };
        context.Users.Add(user);
        var notification = new Notification { UserId = user.Id, Type = "dispatch_ready", Channel = "in_app", Content = "Pedido listo." };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync(CancellationToken.None);
        return (context, user, notification);
    }

    [Fact]
    public async Task MarksOwnNotificationAsRead()
    {
        var (context, user, notification) = await Seed();
        var handler = new MarkNotificationReadCommandHandler(context, new FakeCurrentUserService { UserId = user.Id.ToString() });

        await handler.Handle(new MarkNotificationReadCommand(notification.Id), CancellationToken.None);

        var persisted = await context.Notifications.FindAsync(notification.Id);
        Assert.True(persisted!.IsRead);
        Assert.NotNull(persisted.ReadAt);
    }

    [Fact]
    public async Task AnotherUsersNotification_ThrowsNotFoundException()
    {
        var (context, _, notification) = await Seed();
        var handler = new MarkNotificationReadCommandHandler(context, new FakeCurrentUserService { UserId = Guid.NewGuid().ToString() });

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new MarkNotificationReadCommand(notification.Id), CancellationToken.None));
    }
}
