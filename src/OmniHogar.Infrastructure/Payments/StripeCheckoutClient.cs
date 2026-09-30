using Microsoft.Extensions.Options;
using OmniHogar.Application.Common.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace OmniHogar.Infrastructure.Payments;

/// <summary>
/// Stripe Checkout Session client — replaces the earlier Mercado Pago integration (dropped after
/// persistent sandbox friction: test-buyer-only preferences, http back_urls silently discarded,
/// no payment_id on redirect-back). Stripe fixes all three: <c>customer_email</c> works for any
/// email in test mode, http(s) URLs are both accepted, and the {CHECKOUT_SESSION_ID} template
/// always comes back on the redirect regardless of scheme. See
/// https://docs.stripe.com/checkout/quickstart.
/// </summary>
public class StripeCheckoutClient : IPaymentGatewayClient
{
    // Stripe's zero-decimal currencies take unit_amount as whole units, not cents. COP is NOT
    // one of them (unlike CLP) — it takes centavos, same as USD takes cents. Getting this wrong
    // undercharges by 100x with no error (verified against the live Stripe API).
    // https://docs.stripe.com/currencies#zero-decimal
    private static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga", "pyg", "rwf",
        "ugx", "vnd", "vuv", "xaf", "xof", "xpf",
    };

    private const string Currency = "cop";

    private readonly StripeOptions _options;
    private readonly Lazy<StripeClient> _client;

    public StripeCheckoutClient(IOptions<StripeOptions> options)
    {
        _options = options.Value;
        _client = new Lazy<StripeClient>(() => new StripeClient(_options.SecretKey));
    }

    public async Task<PaymentGatewayPreference> CreatePreferenceAsync(PaymentGatewayPreferenceRequest request, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        // The buyer needs to land back with the session id so GetStatus can confirm the payment;
        // Stripe's {CHECKOUT_SESSION_ID} placeholder is substituted at redirect time.
        var successUrl = AppendQueryParam(request.SuccessUrl, "session_id", "{CHECKOUT_SESSION_ID}");

        // A declined card leaves the buyer on Stripe's own page to retry — it never redirects
        // here. The only way we land on cancel_url is the buyer clicking Stripe's "back" link,
        // i.e. an explicit bail-out. Stripe doesn't expire the session for that on its own (it'd
        // stay "open"/retryable for ~24h), so we flag it here and GetCheckoutStatus marks the
        // payment rejected outright instead of asking Stripe (which would still say "pending").
        var cancelUrl = AppendQueryParam(request.FailureUrl, "cancelled", "1");

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            ClientReferenceId = request.ExternalReference,
            CustomerEmail = string.IsNullOrWhiteSpace(request.PayerEmail) ? null : request.PayerEmail,
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            // PSE/wallet aren't distinct Stripe payment methods for CO in Checkout — every method
            // family funnels through card in this sandbox integration.
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = Currency,
                        UnitAmount = ToStripeAmount(request.UnitPrice, Currency),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.Title,
                            Description = request.Title,
                        },
                    },
                },
            ],
        };

        var service = new SessionService(_client.Value);
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

        return new PaymentGatewayPreference { Id = session.Id, InitPoint = session.Url };
    }

    public async Task<PaymentGatewayPayment> GetPaymentAsync(string paymentId, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var service = new SessionService(_client.Value);
        var session = await service.GetAsync(paymentId, cancellationToken: cancellationToken);

        return ToPayment(session);
    }

    public async Task<PaymentGatewayPayment?> FindLatestPaymentAsync(string externalReference, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        // Stripe's Session List doesn't filter by client_reference_id server-side — this is a
        // best-effort fallback for when the buyer's redirect lost the session_id query param;
        // the normal path always has it, so scanning recent sessions is fine at sandbox scale.
        var service = new SessionService(_client.Value);
        var sessions = await service.ListAsync(new SessionListOptions { Limit = 20 }, cancellationToken: cancellationToken);

        var match = sessions.Data.FirstOrDefault(s => s.ClientReferenceId == externalReference);
        return match is null ? null : ToPayment(match);
    }

    private static PaymentGatewayPayment ToPayment(Session session) => new()
    {
        Id = session.Id,
        ExternalReference = session.ClientReferenceId,
        Status = session.PaymentStatus switch
        {
            "paid" => "approved",
            _ when session.Status == "expired" => "cancelled",
            _ => "pending",
        },
    };

    private static long ToStripeAmount(decimal amount, string currency)
    {
        var rounded = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        return ZeroDecimalCurrencies.Contains(currency) ? (long)rounded : (long)(rounded * 100);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new InvalidOperationException("Stripe no está configurado — falta Stripe:SecretKey.");
        }
    }

    private static string AppendQueryParam(string url, string key, string value)
    {
        var separator = url.Contains('?') ? '&' : '?';
        return $"{url}{separator}{key}={value}";
    }
}
