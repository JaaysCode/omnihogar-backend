namespace OmniHogar.Infrastructure.Email;

/// <summary>Bound from the "Email" configuration section (see appsettings.json / .env).</summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = "smtp-relay.brevo.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>SMTP key from the provider's dashboard (not an API key). Never logged.</summary>
    public string SmtpPassword { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "OmniHogar";
    public string FrontendBaseUrl { get; set; } = "http://localhost:4200";
}
