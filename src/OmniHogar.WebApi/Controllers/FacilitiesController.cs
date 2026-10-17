using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniHogar.Application.Features.InventoryTransfers;
using OmniHogar.Domain.Constants;

namespace OmniHogar.WebApi.Controllers;

/// <summary>Warehouses and points of sale — read-only, for the transfer origin/destination pickers.</summary>
[ApiController]
[Route("api/[controller]")]
public class FacilitiesController : ControllerBase
{
    private readonly ISender _sender;

    public FacilitiesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.InventarioConsultar)]
    public async Task<ActionResult<List<FacilityDto>>> GetAll(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetFacilitiesQuery(), cancellationToken);
    }
}
