using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Tests.TestSupport;

/// <summary>
/// Configurable <see cref="ILlmClient"/> stand-in — no real HTTP call to OpenRouter. Set
/// <see cref="IntentToReturn"/> for whatever the test wants the "model" to have decided.
/// </summary>
public class FakeLlmClient : ILlmClient
{
    public ChatIntent IntentToReturn { get; set; } = new(ChatIntentType.Unknown);

    public string? LastUserMessage { get; private set; }
    public IReadOnlyList<DraftItemSummary>? LastDraft { get; private set; }

    public Task<ChatIntent> InterpretAsync(
        string userMessage, IReadOnlyList<DraftItemSummary> currentDraft, CancellationToken cancellationToken)
    {
        LastUserMessage = userMessage;
        LastDraft = currentDraft;
        return Task.FromResult(IntentToReturn);
    }
}
