using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Infrastructure.Payments;

/// <summary>
/// Plain-REST client for the two Mercado Pago Checkout Pro calls this app needs — no SDK
/// dependency (net10.0 compatibility of the official SDK is unverified, and we only need two
/// endpoints). See https://www.mercadopago.com.co/developers/es/reference/online-payments/checkout-pro/overview.
/// </summary>
public class MercadoPagoClient : IMercadoPagoClient
{
    private readonly HttpClient _httpClient;
    private readonly MercadoPagoOptions _options;

    public MercadoPagoClient(HttpClient httpClient, IOptions<MercadoPagoOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<MercadoPagoPreference> CreatePreferenceAsync(MercadoPagoPreferenceRequest request, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var body = new PreferenceRequestDto
        {
            Items = [new ItemDto { Title = request.Title, Quantity = 1, UnitPrice = request.UnitPrice }],
            Payer = request.PayerEmail is null ? null : new PayerDto { Email = request.PayerEmail },
            ExternalReference = request.ExternalReference,
            BackUrls = new BackUrlsDto { Success = request.SuccessUrl, Failure = request.FailureUrl, Pending = request.PendingUrl },
            // auto_return requires https back_urls — omit it over plain http (local dev) instead
            // of letting Mercado Pago reject the whole preference.
            AutoReturn = request.SuccessUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? "approved" : null,
            NotificationUrl = string.IsNullOrWhiteSpace(request.NotificationUrl) ? null : request.NotificationUrl,
            PaymentMethods = request.ExcludedPaymentTypes.Count == 0
                ? null
                : new PaymentMethodsDto { ExcludedPaymentTypes = [.. request.ExcludedPaymentTypes.Select(id => new PaymentTypeDto { Id = id })] },
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "checkout/preferences")
        {
            Content = JsonContent.Create(body),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<PreferenceResponseDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Mercado Pago devolvió una respuesta vacía al crear la preferencia.");

        return new MercadoPagoPreference
        {
            Id = dto.Id ?? string.Empty,
            InitPoint = !string.IsNullOrWhiteSpace(dto.InitPoint) ? dto.InitPoint! : dto.SandboxInitPoint ?? string.Empty,
        };
    }

    public async Task<MercadoPagoPayment> GetPaymentAsync(string paymentId, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"v1/payments/{Uri.EscapeDataString(paymentId)}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<PaymentResponseDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Mercado Pago devolvió una respuesta vacía al consultar el pago.");

        return new MercadoPagoPayment
        {
            Id = dto.Id?.ToString() ?? paymentId,
            Status = dto.Status ?? string.Empty,
            ExternalReference = dto.ExternalReference,
        };
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            throw new InvalidOperationException(
                "Mercado Pago no está configurado — falta MERCADOPAGO_ACCESS_TOKEN.");
        }
    }

    // ---- Wire DTOs (subset of Mercado Pago's JSON — not the full SDK model) ----

    private class PreferenceRequestDto
    {
        [JsonPropertyName("items")]
        public List<ItemDto> Items { get; set; } = [];

        [JsonPropertyName("payer")]
        public PayerDto? Payer { get; set; }

        [JsonPropertyName("external_reference")]
        public string ExternalReference { get; set; } = string.Empty;

        [JsonPropertyName("back_urls")]
        public BackUrlsDto BackUrls { get; set; } = new();

        [JsonPropertyName("auto_return")]
        public string? AutoReturn { get; set; }

        [JsonPropertyName("notification_url")]
        public string? NotificationUrl { get; set; }

        [JsonPropertyName("payment_methods")]
        public PaymentMethodsDto? PaymentMethods { get; set; }
    }

    private class ItemDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; } = 1;

        [JsonPropertyName("unit_price")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("currency_id")]
        public string CurrencyId { get; set; } = "COP";
    }

    private class PayerDto
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    private class BackUrlsDto
    {
        [JsonPropertyName("success")]
        public string Success { get; set; } = string.Empty;

        [JsonPropertyName("failure")]
        public string Failure { get; set; } = string.Empty;

        [JsonPropertyName("pending")]
        public string Pending { get; set; } = string.Empty;
    }

    private class PaymentMethodsDto
    {
        [JsonPropertyName("excluded_payment_types")]
        public List<PaymentTypeDto> ExcludedPaymentTypes { get; set; } = [];
    }

    private class PaymentTypeDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private class PreferenceResponseDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("init_point")]
        public string? InitPoint { get; set; }

        [JsonPropertyName("sandbox_init_point")]
        public string? SandboxInitPoint { get; set; }
    }

    private class PaymentResponseDto
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("external_reference")]
        public string? ExternalReference { get; set; }
    }
}
