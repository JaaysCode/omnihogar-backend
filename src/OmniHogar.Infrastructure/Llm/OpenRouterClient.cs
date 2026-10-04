using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Infrastructure.Llm;

/// <summary>
/// NLU via OpenRouter's OpenAI-compatible chat-completions API (HU-19). Asks the model to
/// return ONLY a small JSON object describing the customer's intent — never free text — so the
/// backend stays the single source of truth for what actually happens and what the customer
/// reads back. Any network failure or malformed response degrades to
/// <see cref="ChatIntentType.Unknown"/> rather than throwing, so a flaky LLM call never crashes
/// a chat turn.
/// </summary>
public class OpenRouterClient : ILlmClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private const string SystemPrompt = """
        Eres el asistente de compras de OmniHogar. Tu única tarea es interpretar el último
        mensaje del cliente y devolver SOLO un objeto JSON, sin texto adicional, con esta forma:
        {"type": "search_product" | "confirm_order" | "unknown", "productQuery": string o null, "quantity": number o null}

        - "search_product": el cliente menciona o describe un producto que quiere buscar o
          agregar a su pedido. Extrae en "productQuery" el nombre o palabra clave del producto
          (sin cantidades ni relleno). Si menciona una cantidad, ponla en "quantity"; si no,
          deja "quantity" en null.
        - "confirm_order": el cliente confirma que quiere finalizar o confirmar su pedido actual
          (por ejemplo "sí, confírmalo", "eso es todo", "confirma el pedido").
        - "unknown": saludo, charla, o cualquier mensaje que no identifique un producto ni
          confirme el pedido.

        No expliques tu respuesta. No agregues texto antes ni después del JSON.
        """;

    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;

    public OpenRouterClient(HttpClient httpClient, IOptions<OpenRouterOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ChatIntent> InterpretAsync(
        string userMessage, IReadOnlyList<DraftItemSummary> currentDraft, CancellationToken cancellationToken)
    {
        try
        {
            var draftContext = currentDraft.Count == 0
                ? "El pedido en construcción está vacío."
                : "Pedido en construcción: " + string.Join(", ", currentDraft.Select(d => $"{d.Quantity}x {d.ProductName}"));

            var request = new OpenRouterChatRequest(
                _options.Model,
                [
                    new OpenRouterMessage("system", SystemPrompt),
                    new OpenRouterMessage("user", $"{draftContext}\nMensaje del cliente: {userMessage}"),
                ],
                new OpenRouterResponseFormat("json_object"));

            using var httpResponse = await _httpClient.PostAsJsonAsync("chat/completions", request, cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
            {
                return new ChatIntent(ChatIntentType.Unknown);
            }

            var payload = await httpResponse.Content.ReadFromJsonAsync<OpenRouterChatResponse>(cancellationToken);
            var content = payload?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                return new ChatIntent(ChatIntentType.Unknown);
            }

            var intent = JsonSerializer.Deserialize<LlmIntentJson>(content, JsonOptions);
            return ToChatIntent(intent);
        }
        catch (Exception)
        {
            // Network failure, timeout, or malformed JSON from the model — never let a chat
            // turn crash because the LLM is unavailable or misbehaving.
            return new ChatIntent(ChatIntentType.Unknown);
        }
    }

    private static ChatIntent ToChatIntent(LlmIntentJson? intent)
    {
        if (intent is null)
        {
            return new ChatIntent(ChatIntentType.Unknown);
        }

        return intent.Type switch
        {
            "search_product" => new ChatIntent(ChatIntentType.SearchProduct, intent.ProductQuery, intent.Quantity),
            "confirm_order" => new ChatIntent(ChatIntentType.ConfirmOrder),
            _ => new ChatIntent(ChatIntentType.Unknown),
        };
    }

    private record OpenRouterChatRequest(
        string Model,
        List<OpenRouterMessage> Messages,
        [property: JsonPropertyName("response_format")] OpenRouterResponseFormat ResponseFormat);

    private record OpenRouterMessage(string Role, string Content);

    private record OpenRouterResponseFormat(string Type);

    private record OpenRouterChatResponse(List<OpenRouterChoice>? Choices);

    private record OpenRouterChoice(OpenRouterMessage? Message);

    private class LlmIntentJson
    {
        public string Type { get; set; } = string.Empty;
        public string? ProductQuery { get; set; }
        public int? Quantity { get; set; }
    }
}
