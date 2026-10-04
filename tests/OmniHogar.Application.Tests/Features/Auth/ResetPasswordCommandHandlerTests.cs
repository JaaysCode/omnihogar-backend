using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.Auth;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Tests.Features.Auth;

public class ResetPasswordCommandHandlerTests
{
    private static PasswordHasher<User> PasswordHasher => new();

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private static async Task<(InMemoryApplicationDbContext Context, User User, string RawToken)> SeedUserWithToken(
        DateTime? expiresAt = null, DateTime? usedAt = null)
    {
        var context = InMemoryApplicationDbContext.Create();
        var user = new User
        {
            Email = "jane.doe@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            UserType = UserType.customer,
        };
        user.PasswordHash = PasswordHasher.HashPassword(user, "OldPassw0rd!");
        context.Users.Add(user);

        const string rawToken = "raw-reset-token";
        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Hash(rawToken),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(30),
            UsedAt = usedAt,
        });
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, user, rawToken);
    }

    [Fact]
    public async Task ValidToken_UpdatesPasswordAndMarksTokenUsed()
    {
        var (context, user, rawToken) = await SeedUserWithToken();
        var handler = new ResetPasswordCommandHandler(context, PasswordHasher);

        await handler.Handle(new ResetPasswordCommand(rawToken, "NewPassw0rd!"), CancellationToken.None);

        var updatedUser = await context.Users.SingleAsync(u => u.Id == user.Id);
        Assert.Equal(
            PasswordVerificationResult.Success,
            PasswordHasher.VerifyHashedPassword(updatedUser, updatedUser.PasswordHash, "NewPassw0rd!"));

        var token = await context.PasswordResetTokens.SingleAsync(t => t.UserId == user.Id);
        Assert.NotNull(token.UsedAt);
    }

    [Fact]
    public async Task ValidToken_RevokesAllActiveRefreshTokens()
    {
        var (context, user, rawToken) = await SeedUserWithToken();
        context.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = "rt-hash-1", ExpiresAt = DateTime.UtcNow.AddDays(7) });
        context.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = "rt-hash-2", ExpiresAt = DateTime.UtcNow.AddDays(7) });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ResetPasswordCommandHandler(context, PasswordHasher);
        await handler.Handle(new ResetPasswordCommand(rawToken, "NewPassw0rd!"), CancellationToken.None);

        var refreshTokens = await context.RefreshTokens.Where(rt => rt.UserId == user.Id).ToListAsync();
        Assert.All(refreshTokens, rt => Assert.NotNull(rt.RevokedAt));
    }

    [Fact]
    public async Task ExpiredToken_ThrowsNotFoundException()
    {
        var (context, _, rawToken) = await SeedUserWithToken(expiresAt: DateTime.UtcNow.AddMinutes(-1));
        var handler = new ResetPasswordCommandHandler(context, PasswordHasher);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new ResetPasswordCommand(rawToken, "NewPassw0rd!"), CancellationToken.None));
    }

    [Fact]
    public async Task AlreadyUsedToken_ThrowsNotFoundException()
    {
        var (context, _, rawToken) = await SeedUserWithToken(usedAt: DateTime.UtcNow.AddMinutes(-5));
        var handler = new ResetPasswordCommandHandler(context, PasswordHasher);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new ResetPasswordCommand(rawToken, "NewPassw0rd!"), CancellationToken.None));
    }

    [Fact]
    public async Task UnknownToken_ThrowsNotFoundException()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var handler = new ResetPasswordCommandHandler(context, PasswordHasher);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new ResetPasswordCommand("nonexistent", "NewPassw0rd!"), CancellationToken.None));
    }
}
