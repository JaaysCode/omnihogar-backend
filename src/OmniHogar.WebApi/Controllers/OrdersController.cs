using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniHogar.Application.Features.Orders;
using OmniHogar.Domain.Constants;

namespace OmniHogar.WebApi.Controllers;

/// <summary>
/// Cross-channel order consultation/management for advisors and dispatch staff
/// (permission-gated actions), plus self-service endpoints for the authenticated customer's own
/// orders (<c>mine</c>/<c>mine/{id}</c> — plain <c>[Authorize]</c>, ownership-checked in the handler,
/// since the Cliente role has no permission claims).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.PedidosConsultar)]
    public async Task<ActionResult<List<OrderDto>>> GetAll(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetOrdersQuery(), cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AppPermissions.PedidosConsultar)]
    public async Task<ActionResult<OrderDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetOrderByIdQuery(id), cancellationToken);
    }

    /// <summary>Advance an order's status (HU-14 crit. 1) — dispatch coordinator/admin only.</summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = AppPermissions.PedidosActualizarEstado)]
    public async Task<ActionResult<OrderDetailDto>> UpdateStatus(
        Guid id,
        UpdateOrderStatusRequest body,
        CancellationToken cancellationToken)
    {
        return await _sender.Send(new UpdateOrderStatusCommand(id, body.Status, body.Comment), cancellationToken);
    }

    /// <summary>Register an in-store sale (HU-06) — creates the order and decrements stock.</summary>
    [HttpPost]
    [Authorize(Policy = AppPermissions.PosRegistrarVenta)]
    public async Task<ActionResult<StoreSaleResultDto>> RegisterStoreSale(
        RegisterStoreSaleRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RegisterStoreSaleCommand(body.Items), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.OrderId }, result);
    }

    /// <summary>The authenticated customer's own orders (HU-14 crit. 2 — "mis pedidos").</summary>
    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<List<OrderDto>>> GetMine(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetMyOrdersQuery(), cancellationToken);
    }

    /// <summary>Detail of one of the authenticated customer's own orders (HU-14 crit. 2/3).</summary>
    [HttpGet("mine/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<OrderDetailDto>> GetMineById(Guid id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetMyOrderByIdQuery(id), cancellationToken);
    }
}

/// <summary>Request body for <see cref="OrdersController.RegisterStoreSale"/>.</summary>
public record RegisterStoreSaleRequest(IReadOnlyList<StoreSaleItem> Items);

/// <summary>Request body for <see cref="OrdersController.UpdateStatus"/>.</summary>
public record UpdateOrderStatusRequest(string Status, string? Comment);
