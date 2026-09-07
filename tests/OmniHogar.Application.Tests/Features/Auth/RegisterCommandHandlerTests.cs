using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Features.Auth;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Auth;

public class RegisterCommandHandlerTests
{
    private static PasswordHasher<User> PasswordHasher => new();

    [Fact]
    public async Task Handle_PersistsNewCustomerAccount_WithNormalizedEmailAndHashedPassword()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var handler = new RegisterCommandHandler(context, PasswordHasher);
        var command = new RegisterCommand("New.Customer@Example.com", "Passw0rd!", "  Jane ", " Doe ", "3001234567");

        var userId = await handler.Handle(command, CancellationToken.None);

        var stored = await context.Users.SingleAsync(u => u.Id == userId);
        Assert.Equal("new.customer@example.com", stored.Email);
        Assert.Equal("Jane", stored.FirstName);
        Assert.Equal("Doe", stored.LastName);
        Assert.Equal(UserType.customer, stored.UserType);
        Assert.Equal("3001234567", stored.Phone);
        Assert.NotEqual(command.Password, stored.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            PasswordHasher.VerifyHashedPassword(stored, stored.PasswordHash, command.Password));
    }

    [Fact]
    public async Task Handle_WithoutPhone_StoresNullPhone()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var handler = new RegisterCommandHandler(context, PasswordHasher);
        var command = new RegisterCommand("no.phone@example.com", "Passw0rd!", "Jane", "Doe", null);

        var userId = await handler.Handle(command, CancellationToken.None);

        var stored = await context.Users.SingleAsync(u => u.Id == userId);
        Assert.Null(stored.Phone);
    }
}
