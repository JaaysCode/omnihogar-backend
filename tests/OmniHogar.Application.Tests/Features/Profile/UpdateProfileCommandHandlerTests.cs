using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.Profile;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Tests.Features.Profile;

public class UpdateProfileCommandHandlerTests
{
    private static async Task<(InMemoryApplicationDbContext Context, FakeCurrentUserService User, Guid UserId)> Seed()
    {
        var context = InMemoryApplicationDbContext.Create();
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
        return (context, new FakeCurrentUserService { UserId = userId.ToString() }, userId);
    }

    [Fact]
    public async Task ValidData_UpdatesNameEmailAndPhone()
    {
        var (context, user, userId) = await Seed();
        var handler = new UpdateProfileCommandHandler(context, user);

        var result = await handler.Handle(
            new UpdateProfileCommand("Ana María", "Gómez Ruiz", "ana.maria@x.test", "3009876543"), CancellationToken.None);

        Assert.Equal("Ana María", result.FirstName);
        Assert.Equal("Gómez Ruiz", result.LastName);
        Assert.Equal("ana.maria@x.test", result.Email);
        Assert.Equal("3009876543", result.Phone);

        var persisted = await context.Users.SingleAsync(u => u.Id == userId);
        Assert.Equal("Ana María", persisted.FirstName);
        Assert.Equal("ana.maria@x.test", persisted.Email);
        Assert.Equal("3009876543", persisted.Phone);
    }

    [Fact]
    public async Task Email_IsNormalizedToLowercase()
    {
        var (context, user, userId) = await Seed();
        var handler = new UpdateProfileCommandHandler(context, user);

        await handler.Handle(new UpdateProfileCommand("Ana", "Gómez", "Ana.Maria@X.TEST", null), CancellationToken.None);

        var persisted = await context.Users.SingleAsync(u => u.Id == userId);
        Assert.Equal("ana.maria@x.test", persisted.Email);
    }

    [Fact]
    public async Task BlankPhone_ClearsIt()
    {
        var (context, user, userId) = await Seed();
        var handler = new UpdateProfileCommandHandler(context, user);

        await handler.Handle(new UpdateProfileCommand("Ana", "Gómez", "ana@x.test", "   "), CancellationToken.None);

        var persisted = await context.Users.SingleAsync(u => u.Id == userId);
        Assert.Null(persisted.Phone);
    }

    [Fact]
    public async Task UnknownUser_ThrowsNotFound()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var handler = new UpdateProfileCommandHandler(context, new FakeCurrentUserService { UserId = Guid.NewGuid().ToString() });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateProfileCommand("Ana", "Gómez", "ana@x.test", null), CancellationToken.None));
    }
}
