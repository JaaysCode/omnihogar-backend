using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Identity;

public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();

    /// <summary>SHA-256 hash of a raw refresh token, for storage/lookup — the raw value is
    /// never persisted (see <see cref="Domain.Entities.RefreshToken"/>).</summary>
    string HashRefreshToken(string rawToken);
}
