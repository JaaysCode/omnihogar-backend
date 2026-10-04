using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Tests.TestSupport;

/// <summary>Captures "sent" emails instead of hitting real SMTP, for handler tests.</summary>
public class FakeEmailService : IEmailService
{
    public List<(string ToEmail, string ResetToken)> SentResetEmails { get; } = [];

    public Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken cancellationToken)
    {
        SentResetEmails.Add((toEmail, resetToken));
        return Task.CompletedTask;
    }
}
