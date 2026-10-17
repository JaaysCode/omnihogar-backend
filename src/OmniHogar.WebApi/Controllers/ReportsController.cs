using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniHogar.Application.Features.Reports;
using OmniHogar.Domain.Constants;

namespace OmniHogar.WebApi.Controllers;

/// <summary>Sales reports for the administrator (<c>reportes.ver</c>).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AppPermissions.ReportesVer)]
public class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Sales grouped by channel (count and total sold), optionally limited to the
    /// inclusive period <c>from</c>–<c>to</c> (yyyy-MM-dd). 400 when <c>from</c> is after <c>to</c>.</summary>
    [HttpGet("sales-by-channel")]
    public async Task<ActionResult<SalesByChannelReportDto>> GetSalesByChannel(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetSalesByChannelQuery(from, to), cancellationToken);
    }
}
