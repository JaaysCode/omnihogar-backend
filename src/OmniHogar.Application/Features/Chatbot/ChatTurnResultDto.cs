using OmniHogar.Application.Features.Products;

namespace OmniHogar.Application.Features.Chatbot;

/// <summary>One line of the customer's in-progress order, echoed back with name/price filled in.</summary>
public record ChatDraftItemDto(Guid ProductId, string Name, int Quantity, decimal UnitPrice, decimal Subtotal);

/// <summary>Populated only on the turn that confirmed and created the order (HU-19 crit. 2/3).</summary>
public record ChatOrderCreatedDto(Guid OrderId, string OrderNumber, decimal Total);

/// <summary>Result of one chat turn (HU-19).</summary>
public record ChatTurnResultDto(
    Guid ConversationId,
    string Reply,
    List<ChatDraftItemDto> DraftItems,
    List<ProductDto>? MatchedProducts,
    ChatOrderCreatedDto? OrderCreated);
