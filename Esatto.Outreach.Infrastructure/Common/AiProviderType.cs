namespace Esatto.Outreach.Infrastructure.Common;

/// <summary>
/// Enum for selecting which AI provider to use for operations.
/// </summary>
public enum AiProviderType
{
    /// <summary>
    /// Use OpenAI (GPT models with web search via Responses API)
    /// </summary>
    OpenAI,

    /// <summary>
    /// Use Anthropic Claude (with web search via Messages API)
    /// </summary>
    Claude,

    /// <summary>
    /// Use both OpenAI and Claude in parallel, then aggregate and deduplicate results
    /// </summary>
    Hybrid
}
