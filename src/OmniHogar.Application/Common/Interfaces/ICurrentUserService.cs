namespace OmniHogar.Application.Common.Interfaces;

/// <summary>
/// Exposes the authenticated user's identity to Application handlers without
/// depending on ASP.NET Core's HttpContext directly.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
