using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniHogar.Application.Features.Checkout;

namespace OmniHogar.WebApi.Controllers;

/// <summary>
/// Client checkout — turns the cart into an order and starts a Mercado Pago Checkout Pro payment
/// (HU-08/HU-09). No permission policy: any authenticated user, same as <see cref="CartController"/>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly ISender _sender;

    public CheckoutController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize]
    [HttpPost("preference")]
    public async Task<ActionResult<CheckoutDto>> CreatePreference(CreateCheckoutPreferenceRequest body, CancellationToken cancellationToken)
    {
        var command = new CreateCheckoutPreferenceCommand(
            new CustomerAddressInput(body.Address, body.City, body.Neighborhood, body.Reference),
            body.PaymentMethod);
        return await _sender.Send(command, cancellationToken);
    }

    /// <summary>Generates a new payment attempt for an order stuck at pending/rejected (HU-09 crit. 2/3).</summary>
    [Authorize]
    [HttpPost("{orderId:guid}/retry")]
    public async Task<ActionResult<CheckoutDto>> Retry(Guid orderId, CancellationToken cancellationToken)
    {
        return await _sender.Send(new RetryCheckoutPaymentCommand(orderId), cancellationToken);
    }

    [Authorize]
    [HttpGet("{orderId:guid}/status")]
    public async Task<ActionResult<CheckoutStatusDto>> GetStatus(Guid orderId, [FromQuery] string? paymentId, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetCheckoutStatusQuery(orderId, paymentId), cancellationToken);
    }

    /// <summary>Mercado Pago's server-to-server notification — no user session, never our JWT.
    /// Always answers 204 regardless of outcome, per their integration guidance.</summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] MercadoPagoWebhookRequest? body, CancellationToken cancellationToken)
    {
        var type = body?.Type ?? Request.Query["type"].FirstOrDefault() ?? Request.Query["topic"].FirstOrDefault();
        var paymentId = body?.Data?.Id ?? Request.Query["data.id"].FirstOrDefault() ?? Request.Query["id"].FirstOrDefault();

        await _sender.Send(new HandleMercadoPagoWebhookCommand(type, paymentId), cancellationToken);
        return NoContent();
    }
}

/// <summary>Body for <see cref="CheckoutController.CreatePreference"/>.</summary>
public record CreateCheckoutPreferenceRequest(string Address, string City, string? Neighborhood, string? Reference, string PaymentMethod);

/// <summary>Mercado Pago's webhook body shape: <c>{ "type": "payment", "data": { "id": "..." } }</c>.</summary>
public record MercadoPagoWebhookRequest(string? Type, MercadoPagoWebhookData? Data);

public record MercadoPagoWebhookData(string? Id);
