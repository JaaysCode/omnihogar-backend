using Microsoft.AspNetCore.Identity;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Identity;

/// <summary>
/// Adapts the concrete ASP.NET Core Identity hasher to the Application-layer
/// <see cref="IPasswordHasher"/> abstraction. The TUser instance passed to the
/// underlying hasher is unused by the default algorithm, so a throwaway one is fine.
/// </summary>
public class PasswordHasherAdapter : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.IPasswordHasher<User> _inner;

    public PasswordHasherAdapter(Microsoft.AspNetCore.Identity.IPasswordHasher<User> inner)
    {
        _inner = inner;
    }

    public string Hash(string password) => _inner.HashPassword(new User(), password);
}
