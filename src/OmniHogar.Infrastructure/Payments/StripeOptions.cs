using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Infrastructure.Payments;

/// <summary>Bound from the "Stripe" configuration section (see appsettings.json / .env).</summary>
public class StripeOptions : IPaymentGatewayUrls
{
    public const string SectionName = "Stripe";

    /// <summary>Sandbox or live secret key (sk_test_.../sk_live_...). Never logged, never returned to a client.</summary>
    public string SecretKey { get; set; } = string.Empty;

    public string FrontendBaseUrl { get; set; } = "http://localhost:4200";

    public string BackendPublicBaseUrl { get; set; } = "http://localhost:5244";
}
