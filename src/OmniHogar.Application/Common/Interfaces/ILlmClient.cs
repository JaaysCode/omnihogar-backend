namespace OmniHogar.Application.Common.Interfaces;

public enum ChatIntentType
{
    SearchProduct,
    ConfirmOrder,
    Unknown,
}

/// <summary>
/// NLU result for one chat turn — only the fields relevant to <see cref="Type"/> are populated.
/// </summary>
public record ChatIntent(ChatIntentType Type, string? ProductQuery = null, int? Quantity = null);

/// <summary>One line of the customer's in-progress order, given to the LLM as context only —
/// never persisted or mutated by the LLM layer itself.</summary>
public record DraftItemSummary(string ProductName, int Quantity);

/// <summary>
/// Thin abstraction over the LLM provider (OpenRouter) used purely for NLU — turning a
/// customer's free-text chat message into a small structured intent (HU-19). The backend, not
/// the model, decides and executes the resulting action and composes the reply text — this
/// method returns intent only, never user-facing text.
/// </summary>
public interface ILlmClient
{
    Task<ChatIntent> InterpretAsync(
        string userMessage,
        IReadOnlyList<DraftItemSummary> currentDraft,
        CancellationToken cancellationToken);
}
