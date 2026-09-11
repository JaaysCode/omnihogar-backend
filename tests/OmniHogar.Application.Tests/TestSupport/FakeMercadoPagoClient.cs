using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Tests.TestSupport;

/// <summary>
/// Configurable <see cref="IMercadoPagoClient"/> stand-in — no real network calls. Set
/// <see cref="PreferenceToReturn"/>/<see cref="PaymentToReturn"/> for the happy path, or
/// <see cref="ThrowOnCreatePreference"/>/<see cref="ThrowOnGetPayment"/> to simulate a
/// communication error with the gateway.
/// </summary>
public class FakeMercadoPagoClient : IMercadoPagoClient
{
    public MercadoPagoPreference PreferenceToReturn { get; set; } = new() { Id = "pref-1", InitPoint = "https://sandbox.mercadopago.com/checkout/pref-1" };
    public MercadoPagoPayment PaymentToReturn { get; set; } = new() { Id = "pay-1", Status = "approved" };
    public bool ThrowOnCreatePreference { get; set; }
    public bool ThrowOnGetPayment { get; set; }

    public MercadoPagoPreferenceRequest? LastPreferenceRequest { get; private set; }
    public string? LastPaymentIdRequested { get; private set; }

    public Task<MercadoPagoPreference> CreatePreferenceAsync(MercadoPagoPreferenceRequest request, CancellationToken cancellationToken)
    {
        LastPreferenceRequest = request;
        if (ThrowOnCreatePreference)
        {
            throw new InvalidOperationException("Simulated Mercado Pago communication error.");
        }
        return Task.FromResult(PreferenceToReturn);
    }

    public Task<MercadoPagoPayment> GetPaymentAsync(string paymentId, CancellationToken cancellationToken)
    {
        LastPaymentIdRequested = paymentId;
        if (ThrowOnGetPayment)
        {
            throw new InvalidOperationException("Simulated Mercado Pago communication error.");
        }
        return Task.FromResult(PaymentToReturn);
    }
}
