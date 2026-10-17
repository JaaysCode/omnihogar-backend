using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniHogar.Application.Features.InventoryTransfers;
using OmniHogar.Domain.Constants;

namespace OmniHogar.WebApi.Controllers;

/// <summary>
/// Inventory transfers between facilities (jefe de bodega). Consulting needs
/// <c>inventario.consultar</c>; registering, confirming and receiving need <c>inventario.ajustar</c>.
/// </summary>
[ApiController]
[Route("api/inventory-transfers")]
public class InventoryTransfersController : ControllerBase
{
    private readonly ISender _sender;

    public InventoryTransfersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.InventarioConsultar)]
    public async Task<ActionResult<List<InventoryTransferDto>>> GetAll([FromQuery] string? status, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetInventoryTransfersQuery(status), cancellationToken);
    }

    /// <summary>Register a transfer (pending). 400 with a "quantity" error when the origin lacks units.</summary>
    [HttpPost]
    [Authorize(Policy = AppPermissions.InventarioAjustar)]
    public async Task<ActionResult<InventoryTransferDto>> Create(CreateInventoryTransferRequest body, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateInventoryTransferCommand(body.ProductId, body.SourceFacilityId, body.DestinationFacilityId, body.Quantity),
            cancellationToken);
        return CreatedAtAction(nameof(GetAll), result);
    }

    /// <summary>Confirm a pending transfer — deducts the units from the origin.</summary>
    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = AppPermissions.InventarioAjustar)]
    public async Task<ActionResult<InventoryTransferDto>> Confirm(Guid id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new ConfirmInventoryTransferCommand(id), cancellationToken);
    }

    /// <summary>Mark an in-transit transfer as received — credits the units to the destination.</summary>
    [HttpPost("{id:guid}/receive")]
    [Authorize(Policy = AppPermissions.InventarioAjustar)]
    public async Task<ActionResult<InventoryTransferDto>> Receive(Guid id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new ReceiveInventoryTransferCommand(id), cancellationToken);
    }
}

/// <summary>Request body for <see cref="InventoryTransfersController.Create"/>.</summary>
public record CreateInventoryTransferRequest(Guid ProductId, Guid SourceFacilityId, Guid DestinationFacilityId, int Quantity);
