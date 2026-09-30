namespace OmniHogar.Application.Common.Interfaces;

/// <summary>
/// The public URLs the checkout flow needs to hand the payment gateway (where to send the buyer
/// back, where to POST webhook notifications). Kept separate from <see cref="IPaymentGatewayClient"/>
/// so the secret key stays Infrastructure-only.
/// </summary>
public interface IPaymentGatewayUrls
{
    /// <summary>Base URL of the Angular app — success/cancel URLs point at "{FrontendBaseUrl}/checkout/result".</summary>
    string FrontendBaseUrl { get; }

    /// <summary>Publicly reachable base URL of this API — notification_url points at
    /// "{BackendPublicBaseUrl}/api/checkout/webhook". The gateway's servers call this directly,
    /// so in local dev it only works behind a tunnel (e.g. ngrok); the redirect-based
    /// confirmation path doesn't depend on this being reachable.</summary>
    string BackendPublicBaseUrl { get; }
}
