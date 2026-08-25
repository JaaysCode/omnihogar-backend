namespace OmniHogar.Domain.Entities;

/// <summary>Single message within a conversation (messages).</summary>
public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    /// <summary>Allowed: customer, bot, advisor.</summary>
    public string Sender { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
