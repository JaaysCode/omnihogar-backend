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

    /// <summary>Cross-channel order list, optionally filtered to one status (HU-12 — e.g.
    /// <c>?status=preparing</c> for the dispatch coordinator's pending-preparation view).</summary>
    [HttpGet]
    [Authorize(Policy = AppPermissions.PedidosConsultar)]
    public async Task<ActionResult<List<OrderDto>>> GetAll([FromQuery] string? status, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetOrdersQuery(status), cancellationToken);
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

    /// <summary>Generate the shipping guide of a packed order (HU-25) — dispatch coordinator/admin only.</summary>
    [HttpPost("{id:guid}/shipping-label")]
    [Authorize(Policy = AppPermissions.PedidosActualizarEstado)]
    public async Task<ActionResult<ShippingLabelDto>> GenerateShippingLabel(Guid id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GenerateShippingLabelCommand(id), cancellationToken);
    }

    /// <summary>Consult the shipping guide already generated for an order (HU-25 crit. 2).</summary>
    [HttpGet("{id:guid}/shipping-label")]
    [Authorize(Policy = AppPermissions.PedidosConsultar)]
    public async Task<ActionResult<ShippingLabelDto>> GetShippingLabel(Guid id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetShippingLabelQuery(id), cancellationToken);
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

    /// <summary>Tracking (current status + status trail) of one of the authenticated customer's
    /// own orders (HU-14 seguimiento). 404 when the order doesn't exist or isn't theirs.</summary>
    [HttpGet("mine/{id:guid}/tracking")]
    [Authorize]
    public async Task<ActionResult<OrderTrackingDto>> GetMineTracking(Guid id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetMyOrderTrackingQuery(id), cancellationToken);
    }

    /// <summary>Payment receipt of one of the authenticated customer's own orders (HU-20). 400
    /// with the transaction status when the payment was not approved; 404 when it isn't theirs.</summary>
    [HttpGet("mine/{id:guid}/receipt")]
    [Authorize]
    public async Task<ActionResult<OrderReceiptDto>> GetMineReceipt(Guid id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetMyOrderReceiptQuery(id), cancellationToken);
    }
}

/// <summary>Request body for <see cref="OrdersController.RegisterStoreSale"/>.</summary>
public record RegisterStoreSaleRequest(IReadOnlyList<StoreSaleItem> Items);

/// <summary>Request body for <see cref="OrdersController.UpdateStatus"/>.</summary>
public record UpdateOrderStatusRequest(string Status, string? Comment);
