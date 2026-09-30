using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Tests.TestSupport;

/// <summary>
/// Configurable <see cref="IPaymentGatewayClient"/> stand-in — no real network calls. Set
/// <see cref="PreferenceToReturn"/>/<see cref="PaymentToReturn"/> for the happy path, or
/// <see cref="ThrowOnCreatePreference"/>/<see cref="ThrowOnGetPayment"/> to simulate a
/// communication error with the gateway.
/// </summary>
public class FakePaymentGatewayClient : IPaymentGatewayClient
{
    public PaymentGatewayPreference PreferenceToReturn { get; set; } = new() { Id = "pref-1", InitPoint = "https://checkout.stripe.com/c/pay/cs_test_pref-1" };
    public PaymentGatewayPayment PaymentToReturn { get; set; } = new() { Id = "pay-1", Status = "approved" };
    public bool ThrowOnCreatePreference { get; set; }
    public bool ThrowOnGetPayment { get; set; }

    public PaymentGatewayPreferenceRequest? LastPreferenceRequest { get; private set; }
    public string? LastPaymentIdRequested { get; private set; }

    public Task<PaymentGatewayPreference> CreatePreferenceAsync(PaymentGatewayPreferenceRequest request, CancellationToken cancellationToken)
    {
        LastPreferenceRequest = request;
        if (ThrowOnCreatePreference)
        {
            throw new InvalidOperationException("Simulated payment gateway communication error.");
        }
        return Task.FromResult(PreferenceToReturn);
    }

    public Task<PaymentGatewayPayment> GetPaymentAsync(string paymentId, CancellationToken cancellationToken)
    {
        LastPaymentIdRequested = paymentId;
        if (ThrowOnGetPayment)
        {
            throw new InvalidOperationException("Simulated payment gateway communication error.");
        }
        return Task.FromResult(PaymentToReturn);
    }

    public PaymentGatewayPayment? LatestPaymentToReturn { get; set; }
    public string? LastExternalReferenceSearched { get; private set; }

    public Task<PaymentGatewayPayment?> FindLatestPaymentAsync(string externalReference, CancellationToken cancellationToken)
    {
        LastExternalReferenceSearched = externalReference;
        if (ThrowOnGetPayment)
        {
            throw new InvalidOperationException("Simulated payment gateway communication error.");
        }
        return Task.FromResult(LatestPaymentToReturn);
    }
}
