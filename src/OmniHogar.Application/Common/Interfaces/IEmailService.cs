namespace OmniHogar.Application.Common.Interfaces;

/// <summary>
/// Thin abstraction over outbound transactional email, so Application handlers never depend on
/// SMTP/MailKit specifics. Implemented in Infrastructure via an SMTP relay.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends the password-recovery email. <paramref name="resetToken"/> is the raw (unhashed)
    /// token — the implementation builds the full reset-link URL itself (it owns the frontend
    /// base URL config), so Application never needs to know that value.
    /// </summary>
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken cancellationToken);
}
