using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniHogar.Application.Features.Employees;
using OmniHogar.Application.Features.Roles;
using OmniHogar.Domain.Constants;

namespace OmniHogar.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AppPermissions.UsuariosGestionar)]
public class RolesController : ControllerBase
{
    private readonly ISender _sender;

    public RolesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Roles with the permissions each one grants (HU-31 crit. 1).</summary>
    [HttpGet]
    public async Task<ActionResult<List<RoleDto>>> GetAll(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetRolesQuery(), cancellationToken);
    }

    /// <summary>The full permission catalogue, for the role-permissions editor (HU-31).</summary>
    [HttpGet("permissions")]
    public async Task<ActionResult<List<PermissionDto>>> GetPermissions(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetPermissionsQuery(), cancellationToken);
    }

    /// <summary>Replace a role's permission set (HU-31 crit. 3).</summary>
    [HttpPut("{id:guid}/permissions")]
    public async Task<IActionResult> UpdatePermissions(Guid id, SetRolePermissionsRequest body, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateRolePermissionsCommand(id, body.Permissions), cancellationToken);
        return NoContent();
    }
}

/// <summary>Request body for <see cref="RolesController.UpdatePermissions"/> — RoleId comes from the route.</summary>
public record SetRolePermissionsRequest(IReadOnlyList<string> Permissions);
