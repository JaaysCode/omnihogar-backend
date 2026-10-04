namespace OmniHogar.Domain.Entities;

/// <summary>
/// Opaque password-reset token (password_reset_tokens), mirroring RefreshToken: the raw token is
/// only ever emailed to the user — this row stores its SHA-256 hash, never the raw value.
/// Single-use via <see cref="UsedAt"/>, separate from <see cref="ExpiresAt"/>.
/// </summary>
public class PasswordResetToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UsedAt { get; set; }
}
