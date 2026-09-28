using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniHogar.Application.Features.Profile;

namespace OmniHogar.WebApi.Controllers;

/// <summary>Own-account profile (HU-16). Just `[Authorize]` — no specific permission policy,
/// since every role (cliente, administrador, jefe de bodega, coordinador de despacho, asesor de
/// tienda) can view/edit its own data.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly ISender _sender;

    public ProfileController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetMine(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetMyProfileQuery(), cancellationToken);
    }

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> UpdateMine(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        return await _sender.Send(command, cancellationToken);
    }
}
