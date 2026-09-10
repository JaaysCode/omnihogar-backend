using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniHogar.Application.Features.Employees;
using OmniHogar.Domain.Constants;

namespace OmniHogar.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AppPermissions.UsuariosGestionar)]
public class EmployeesController : ControllerBase
{
    private readonly ISender _sender;

    public EmployeesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetEmployeesQuery(), cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetEmployeeByIdQuery(id), cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        var id = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Create), new { id }, id);
    }

    /// <summary>Reassign an existing employee to a different role (HU-31 crit. 2).</summary>
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, SetEmployeeRoleRequest body, CancellationToken cancellationToken)
    {
        await _sender.Send(new ChangeEmployeeRoleCommand(id, body.RoleId), cancellationToken);
        return NoContent();
    }
}

/// <summary>Request body for <see cref="EmployeesController.ChangeRole"/> — EmployeeId comes from the route.</summary>
public record SetEmployeeRoleRequest(Guid RoleId);
