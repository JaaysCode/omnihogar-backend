using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Infrastructure.Email;

/// <summary>Sends transactional email via an SMTP relay (MailKit).</summary>
public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public SmtpEmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken cancellationToken)
    {
        var resetLink = $"{_settings.FrontendBaseUrl}/reset-password" +
            $"?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(toEmail)}";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Recupera tu contraseña en OmniHogar";
        message.Body = new TextPart("plain")
        {
            Text = $"""
                Recibimos una solicitud para restablecer tu contraseña.

                Haz clic en el siguiente enlace para crear una nueva contraseña (válido por 30 minutos):
                {resetLink}

                Si no solicitaste este cambio, puedes ignorar este correo.
                """,
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
