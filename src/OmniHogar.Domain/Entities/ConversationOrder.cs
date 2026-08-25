namespace OmniHogar.Domain.Entities;

/// <summary>Link between a conversation and an order it references (conversation_orders, composite key).</summary>
public class ConversationOrder
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
}
