namespace OmniHogar.Domain.Entities;

/// <summary>
/// Opaque refresh token issued at login/refresh (refresh_tokens). The raw token is only ever
/// handed to the client — this row stores its SHA-256 hash, never the raw value, so a leaked
/// database dump doesn't hand out usable tokens.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    /// <summary>Set when this token was exchanged for a new one (rotation on refresh). Lets a
    /// reused, already-rotated token be recognized as a replay rather than accepted again.</summary>
    public Guid? ReplacedByTokenId { get; set; }
}
