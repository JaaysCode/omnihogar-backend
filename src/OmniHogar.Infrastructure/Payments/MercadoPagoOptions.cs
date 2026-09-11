using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Infrastructure.Payments;

/// <summary>Bound from the "MercadoPago" configuration section (see appsettings.json / .env).</summary>
public class MercadoPagoOptions : IMercadoPagoUrls
{
    public const string SectionName = "MercadoPago";

    /// <summary>Sandbox or production Access Token. Never logged, never returned to a client.</summary>
    public string AccessToken { get; set; } = string.Empty;

    public string FrontendBaseUrl { get; set; } = "http://localhost:4200";

    public string BackendPublicBaseUrl { get; set; } = "http://localhost:5244";
}
