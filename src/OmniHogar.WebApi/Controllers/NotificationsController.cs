using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniHogar.Application.Features.Notifications;

namespace OmniHogar.WebApi.Controllers;

/// <summary>
/// The authenticated user's in-app notifications — topbar bell (HU-13). Any authenticated user
/// has one, same as <see cref="CartController"/> — no permission policy.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetMine(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetMyNotificationsQuery(), cancellationToken);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkNotificationReadCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkAllNotificationsReadCommand(), cancellationToken);
        return NoContent();
    }
}
