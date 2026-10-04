using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.Auth;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Auth;

public class ForgotPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_PersistsHashedTokenAndSendsOneEmail()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var user = new User
        {
            Email = "jane.doe@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            UserType = UserType.customer,
            PasswordHash = "hash",
        };
        context.Users.Add(user);
        await context.SaveChangesAsync(CancellationToken.None);

        var emailService = new FakeEmailService();
        var handler = new ForgotPasswordCommandHandler(context, emailService);

        await handler.Handle(new ForgotPasswordCommand("jane.doe@example.com"), CancellationToken.None);

        var token = await context.PasswordResetTokens.SingleAsync(t => t.UserId == user.Id);
        Assert.False(string.IsNullOrWhiteSpace(token.TokenHash));
        Assert.True(token.ExpiresAt > DateTime.UtcNow.AddMinutes(25));
        Assert.True(token.ExpiresAt <= DateTime.UtcNow.AddMinutes(30));

        var sent = Assert.Single(emailService.SentResetEmails);
        Assert.Equal("jane.doe@example.com", sent.ToEmail);
        Assert.False(string.IsNullOrWhiteSpace(sent.ResetToken));
        Assert.NotEqual(sent.ResetToken, token.TokenHash); // raw token, not the persisted hash
    }
}
