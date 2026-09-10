using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniHogar.Application.Features.Cart;

namespace OmniHogar.WebApi.Controllers;

/// <summary>
/// The client's shopping cart (HU-05). Any authenticated user has one — no permission policy,
/// since the "Cliente" role carries no <c>permission</c> claims. Every mutation returns the
/// full updated cart so the client can refresh its state in a single round-trip.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ISender _sender;

    public CartController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> Get(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetCartQuery(), cancellationToken);
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(AddCartItemRequest body, CancellationToken cancellationToken)
    {
        await _sender.Send(new AddCartItemCommand(body.ProductId, body.Quantity), cancellationToken);
        return await _sender.Send(new GetCartQuery(), cancellationToken);
    }

    [HttpPut("items/{productId:guid}")]
    public async Task<ActionResult<CartDto>> SetItemQuantity(
        Guid productId,
        SetCartItemQuantityRequest body,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateCartItemQuantityCommand(productId, body.Quantity), cancellationToken);
        return await _sender.Send(new GetCartQuery(), cancellationToken);
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<ActionResult<CartDto>> RemoveItem(Guid productId, CancellationToken cancellationToken)
    {
        await _sender.Send(new RemoveCartItemCommand(productId), cancellationToken);
        return await _sender.Send(new GetCartQuery(), cancellationToken);
    }
}

/// <summary>Body for <see cref="CartController.AddItem"/>.</summary>
public record AddCartItemRequest(Guid ProductId, int Quantity);

/// <summary>Body for <see cref="CartController.SetItemQuantity"/> — ProductId comes from the route.</summary>
public record SetCartItemQuantityRequest(int Quantity);
