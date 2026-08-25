using OmniHogar.Domain.Entities;

namespace OmniHogar.Infrastructure.Identity;

public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
}
