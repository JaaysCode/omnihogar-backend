namespace OmniHogar.Application.Common.Interfaces;

/// <summary>
/// Thin abstraction over Mercado Pago's Checkout Pro REST API (preferences + payment lookup),
/// so Application handlers never depend on HttpClient/JSON specifics directly. Implemented in
/// Infrastructure with a plain HttpClient — no SDK dependency.
/// </summary>
public interface IMercadoPagoClient
{
    Task<MercadoPagoPreference> CreatePreferenceAsync(MercadoPagoPreferenceRequest request, CancellationToken cancellationToken);

    Task<MercadoPagoPayment> GetPaymentAsync(string paymentId, CancellationToken cancellationToken);
}

/// <summary>What we need to create a Checkout Pro preference — one synthetic line item for the
/// whole order total, to avoid rounding drift between our IVA calc and Mercado Pago's sum.</summary>
public class MercadoPagoPreferenceRequest
{
    public string Title { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string ExternalReference { get; set; } = string.Empty;
    public string? PayerEmail { get; set; }
    public string SuccessUrl { get; set; } = string.Empty;
    public string FailureUrl { get; set; } = string.Empty;
    public string PendingUrl { get; set; } = string.Empty;
    public string NotificationUrl { get; set; } = string.Empty;

    /// <summary>Mercado Pago payment_type ids to hide, so only the chosen method family shows.</summary>
    public IReadOnlyList<string> ExcludedPaymentTypes { get; set; } = [];
}

public class MercadoPagoPreference
{
    public string Id { get; set; } = string.Empty;
    public string InitPoint { get; set; } = string.Empty;
}

/// <summary>The subset of a Mercado Pago payment resource we act on.</summary>
public class MercadoPagoPayment
{
    public string Id { get; set; } = string.Empty;

    /// <summary>Mercado Pago's raw status: approved, rejected, pending, in_process, cancelled, etc.</summary>
    public string Status { get; set; } = string.Empty;

    public string? ExternalReference { get; set; }
}
