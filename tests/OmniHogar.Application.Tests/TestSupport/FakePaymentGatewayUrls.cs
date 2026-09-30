using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Tests.TestSupport;

public class FakePaymentGatewayUrls : IPaymentGatewayUrls
{
    public string FrontendBaseUrl { get; set; } = "http://localhost:4200";
    public string BackendPublicBaseUrl { get; set; } = "http://localhost:5244";
}
