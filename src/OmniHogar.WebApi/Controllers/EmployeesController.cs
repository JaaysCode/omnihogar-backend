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

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        var id = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Create), new { id }, id);
    }
}
