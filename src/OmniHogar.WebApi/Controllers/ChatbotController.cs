using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniHogar.Application.Features.Chatbot;

namespace OmniHogar.WebApi.Controllers;

/// <summary>
/// Customer-facing shopping-assistant chat (HU-19). No permission policy: any authenticated
/// user, same as CheckoutController/OrdersController's "mine" endpoints — the Cliente role has
/// no permission claims.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatbotController : ControllerBase
{
    private readonly ISender _sender;

    public ChatbotController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("messages")]
    [Authorize]
    public async Task<ActionResult<ChatTurnResultDto>> SendMessage(SendChatMessageRequest body, CancellationToken cancellationToken)
    {
        var command = new SendChatMessageCommand(body.ConversationId, body.Message, body.DraftItems);
        return await _sender.Send(command, cancellationToken);
    }
}

/// <summary>Request body for <see cref="ChatbotController.SendMessage"/>.</summary>
public record SendChatMessageRequest(Guid? ConversationId, string Message, IReadOnlyList<DraftItemInput> DraftItems);
