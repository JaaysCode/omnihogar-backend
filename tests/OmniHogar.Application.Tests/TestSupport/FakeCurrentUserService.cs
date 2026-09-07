using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Tests.TestSupport;

/// <summary>Minimal <see cref="ICurrentUserService"/> stand-in for handlers that stamp the
/// authenticated user onto what they write (e.g. inventory movements).</summary>
public class FakeCurrentUserService : ICurrentUserService
{
    public string? UserId { get; set; } = Guid.NewGuid().ToString();
    public string? Email { get; set; } = "admin@omnihogar.com";
    public bool IsAuthenticated { get; set; } = true;
}
