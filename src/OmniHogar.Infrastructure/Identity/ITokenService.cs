using System.Security.Claims;

namespace OmniHogar.Infrastructure.Identity;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);
    string GenerateRefreshToken();
}
