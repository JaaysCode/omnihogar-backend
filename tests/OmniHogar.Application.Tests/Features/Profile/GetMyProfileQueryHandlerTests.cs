using OmniHogar.Application.Features.Profile;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Tests.Features.Profile;

public class GetMyProfileQueryHandlerTests
{
    [Fact]
    public async Task ReturnsTheAuthenticatedUsersOwnData()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            UserType = UserType.customer,
            FirstName = "Ana",
            LastName = "Gómez",
            Email = "ana@x.test",
            Phone = "3001234567",
            PasswordHash = "h",
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyProfileQueryHandler(context, new FakeCurrentUserService { UserId = userId.ToString() });
        var result = await handler.Handle(new GetMyProfileQuery(), CancellationToken.None);

        Assert.Equal(userId, result.Id);
        Assert.Equal("Ana", result.FirstName);
        Assert.Equal("Gómez", result.LastName);
        Assert.Equal("ana@x.test", result.Email);
        Assert.Equal("3001234567", result.Phone);
    }

    [Fact]
    public async Task UnknownUser_ThrowsNotFound()
    {
        await using var context = InMemoryApplicationDbContext.Create();

        var handler = new GetMyProfileQueryHandler(context, new FakeCurrentUserService { UserId = Guid.NewGuid().ToString() });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetMyProfileQuery(), CancellationToken.None));
    }
}
