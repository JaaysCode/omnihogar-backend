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
        message.Body = new BodyBuilder
        {
            TextBody = $"""
                Recibimos una solicitud para restablecer tu contraseña.

                Haz clic en el siguiente enlace para crear una nueva contraseña (válido por 30 minutos):
                {resetLink}

                Si no solicitaste este cambio, puedes ignorar este correo.
                """,
            HtmlBody = BuildPasswordResetHtml(resetLink),
        }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    // Brand palette mirrors the frontend's _tokens.scss (primary #2164E8, secondary #324A6D,
    // tertiary #58C8E6, neutral #F5F7F9). Inline styles + table layout: the only thing mail
    // clients reliably render.
    private static string BuildPasswordResetHtml(string resetLink)
    {
        var link = System.Net.WebUtility.HtmlEncode(resetLink);

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Recupera tu contraseña en OmniHogar</title>
            </head>
            <body style="margin:0;padding:0;background-color:#F5F7F9;">
              <div style="display:none;max-height:0;overflow:hidden;opacity:0;">
                Crea una nueva contraseña para tu cuenta de OmniHogar. El enlace es válido por 30 minutos.
              </div>
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#F5F7F9;padding:32px 16px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background-color:#FFFFFF;border:1px solid #E3E8EE;border-radius:16px;overflow:hidden;font-family:'Plus Jakarta Sans','Segoe UI',Arial,sans-serif;">
                      <tr>
                        <td style="background-color:#2164E8;padding:24px 32px;color:#FFFFFF;font-size:22px;font-weight:800;letter-spacing:-0.01em;">
                          OmniHogar
                        </td>
                      </tr>
                      <tr>
                        <td style="background-color:#58C8E6;height:4px;font-size:0;line-height:0;">&nbsp;</td>
                      </tr>
                      <tr>
                        <td style="padding:32px;">
                          <h1 style="margin:0 0 12px;font-size:24px;font-weight:800;color:#324A6D;">Recupera tu contraseña</h1>
                          <p style="margin:0 0 24px;font-size:16px;line-height:1.5;color:#1F2937;">
                            Recibimos una solicitud para restablecer la contraseña de tu cuenta.
                            Haz clic en el botón para crear una nueva.
                          </p>
                          <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 0 24px;">
                            <tr>
                              <td style="background-color:#2164E8;border-radius:10px;">
                                <a href="{link}" style="display:inline-block;padding:14px 28px;font-size:16px;font-weight:700;color:#FFFFFF;text-decoration:none;">Crear nueva contraseña</a>
                              </td>
                            </tr>
                          </table>
                          <p style="margin:0 0 24px;font-size:14px;line-height:1.5;color:#324A6D;">
                            Este enlace es válido por <strong>30 minutos</strong>.
                          </p>
                          <p style="margin:0 0 4px;font-size:13px;line-height:1.5;color:#6B7280;">
                            Si el botón no funciona, copia y pega este enlace en tu navegador:
                          </p>
                          <p style="margin:0;font-size:13px;line-height:1.5;word-break:break-all;">
                            <a href="{link}" style="color:#2164E8;">{link}</a>
                          </p>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:20px 32px;background-color:#F5F7F9;border-top:1px solid #E3E8EE;font-size:13px;line-height:1.5;color:#6B7280;">
                          Si no solicitaste este cambio, puedes ignorar este correo: tu contraseña seguirá siendo la misma.
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
