namespace Esatto.Outreach.Infrastructure.Common;

/// <summary>
/// Enum for selecting which method to use for email generation.
/// </summary>
public enum EmailGenerationType
{
    /// <summary>
    /// Use web search to find real-time information about the prospect, with openAI
    /// </summary>
    WebSearch,

    /// <summary>
    /// Use previously collected data (soft data, hard data) to generate the email, with openAI
    /// </summary>
    UseCollectedData,

    /// <summary>
    /// Use previously collected data (soft data, hard data) to generate the email, on our own RAG + fine-tuned modell
    /// </summary>
    EsattoRag
}
