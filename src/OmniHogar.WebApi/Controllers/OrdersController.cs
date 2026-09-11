using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniHogar.Application.Features.Orders;
using OmniHogar.Domain.Constants;

namespace OmniHogar.WebApi.Controllers;

/// <summary>Cross-channel order consultation for advisors (centralized purchase management).</summary>
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
}

/// <summary>Request body for <see cref="OrdersController.RegisterStoreSale"/>.</summary>
public record RegisterStoreSaleRequest(IReadOnlyList<StoreSaleItem> Items);
