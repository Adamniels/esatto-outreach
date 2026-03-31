namespace Esatto.Outreach.Domain.Enums;

/// <summary>
/// Enum for selecting which method to use for AI outreach generation.
/// </summary>
public enum OutreachGenerationType
{
    /// <summary>
    /// Use web search to find real-time information about the prospect, with openAI
    /// </summary>
    WebSearch,

    /// <summary>
    /// Use previously collected data (soft data, hard data) to generate the email, with openAI
    /// </summary>
    UseCollectedData,
}
