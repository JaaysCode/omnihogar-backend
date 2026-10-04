namespace OmniHogar.Infrastructure.Llm;

/// <summary>Bound from the "OpenRouter" configuration section (see appsettings.json / .env).</summary>
public class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    /// <summary>OpenRouter API key (openrouter.ai account, free tier). Never logged.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// A free-tier (":free"-suffixed) OpenRouter model id. Free-model availability rotates —
    /// verify the current catalog at https://openrouter.ai/models?max_price=0 and confirm the
    /// chosen model reliably follows a "respond with JSON only" instruction before relying on
    /// it in production. Do not treat this default as settled.
    /// </summary>
    public string Model { get; set; } = "REPLACE_ME_VERIFY_FREE_MODEL_ID";

    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
}
