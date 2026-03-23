namespace Esatto.Outreach.Domain.Enums;

/// <summary>
/// Defines the strategy used to generate content (emails, LinkedIn messages, etc.) for workflow steps.
/// </summary>
public enum ContentGenerationStrategy
{
    /// <summary>
    /// Use AI with web search capabilities to generate content.
    /// </summary>
    WebSearch,

    /// <summary>
    /// Use pre-collected Entity Intelligence data to generate content.
    /// Requires that the prospect has been enriched.
    /// </summary>
    UseCollectedData,
}
