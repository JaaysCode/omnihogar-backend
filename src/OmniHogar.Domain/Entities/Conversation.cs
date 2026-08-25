namespace OmniHogar.Domain.Entities;

/// <summary>Chat thread with a customer across a social/chat channel (conversations).</summary>
public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Null until the customer is identified.</summary>
    public Guid? CustomerId { get; set; }
    public User? Customer { get; set; }

    /// <summary>Allowed: whatsapp, messenger, instagram, web.</summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>Allowed: open, escalated, closed.</summary>
    public string Status { get; set; } = "open";

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ConversationOrder> ConversationOrders { get; set; } = new List<ConversationOrder>();
}
