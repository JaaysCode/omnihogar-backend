using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniHogar.Application.Features.Checkout;

namespace OmniHogar.WebApi.Controllers;

/// <summary>
/// Client checkout — turns the cart into an order and starts a Stripe Checkout payment
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
    public async Task<ActionResult<CheckoutStatusDto>> GetStatus(
        Guid orderId, [FromQuery] string? paymentId, [FromQuery] bool cancelled, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetCheckoutStatusQuery(orderId, paymentId, cancelled), cancellationToken);
    }

    /// <summary>Stripe's server-to-server notification (configured in Dashboard > Webhooks) — no
    /// user session, never our JWT. Always answers 204 regardless of outcome; not currently
    /// reachable in local dev (no public tunnel), so the redirect-back <see cref="GetCheckoutStatusQuery"/>
    /// is the confirmation path actually exercised. Signature verification is intentionally
    /// skipped for the same reason — add it back (Stripe-Signature header + webhook secret)
    /// before pointing this at anything real.</summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] StripeWebhookRequest? body, CancellationToken cancellationToken)
    {
        var type = body?.Type ?? Request.Query["type"].FirstOrDefault();
        var paymentId = body?.Data?.Object?.Id ?? Request.Query["id"].FirstOrDefault();

        await _sender.Send(new HandlePaymentGatewayWebhookCommand(type, paymentId), cancellationToken);
        return NoContent();
    }
}

/// <summary>Body for <see cref="CheckoutController.CreatePreference"/>.</summary>
public record CreateCheckoutPreferenceRequest(string Address, string City, string? Neighborhood, string? Reference, string PaymentMethod);

/// <summary>Stripe's webhook event shape: <c>{ "type": "checkout.session.completed", "data": { "object": { "id": "cs_..." } } }</c>.</summary>
public record StripeWebhookRequest(string? Type, StripeWebhookData? Data);

public record StripeWebhookData(StripeWebhookObject? Object);

public record StripeWebhookObject(string? Id);
