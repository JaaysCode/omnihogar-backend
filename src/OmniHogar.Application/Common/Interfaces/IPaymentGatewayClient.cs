namespace OmniHogar.Application.Common.Interfaces;

/// <summary>
/// Thin abstraction over the payment gateway's checkout API (preferences + payment lookup), so
/// Application handlers never depend on gateway SDK/HTTP specifics directly. Implemented in
/// Infrastructure — currently <c>StripeCheckoutClient</c>, backed by the Stripe.net SDK.
/// </summary>
public interface IPaymentGatewayClient
{
    Task<PaymentGatewayPreference> CreatePreferenceAsync(PaymentGatewayPreferenceRequest request, CancellationToken cancellationToken);

    Task<PaymentGatewayPayment> GetPaymentAsync(string paymentId, CancellationToken cancellationToken);

    /// <summary>Most recent payment carrying this external_reference, or null if none yet.
    /// Lets us confirm a payment when the buyer comes back without a payment_id.</summary>
    Task<PaymentGatewayPayment?> FindLatestPaymentAsync(string externalReference, CancellationToken cancellationToken);
}

/// <summary>What we need to create a checkout preference — one synthetic line item for the
/// whole order total, to avoid rounding drift between our line sums and the gateway's own.</summary>
public class PaymentGatewayPreferenceRequest
{
    public string Title { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string ExternalReference { get; set; } = string.Empty;
    public string? PayerEmail { get; set; }
    public string SuccessUrl { get; set; } = string.Empty;
    public string FailureUrl { get; set; } = string.Empty;
    public string PendingUrl { get; set; } = string.Empty;
    public string NotificationUrl { get; set; } = string.Empty;
}

public class PaymentGatewayPreference
{
    public string Id { get; set; } = string.Empty;
    public string InitPoint { get; set; } = string.Empty;
}

/// <summary>The subset of a gateway payment resource we act on.</summary>
public class PaymentGatewayPayment
{
    public string Id { get; set; } = string.Empty;

    /// <summary>Normalized status: approved, rejected, cancelled, or pending (anything else
    /// still awaiting a final answer) — see <c>StripeCheckoutClient.ToPayment</c> for the
    /// mapping from Stripe's own session status.</summary>
    public string Status { get; set; } = string.Empty;

    public string? ExternalReference { get; set; }
}
