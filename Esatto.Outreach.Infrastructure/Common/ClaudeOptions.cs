namespace Esatto.Outreach.Infrastructure.Common;

/// <summary>
/// Configuration options for Anthropic Claude API.
/// </summary>
public sealed class ClaudeOptions
{
    public const string SectionName = "Claude";

    /// <summary>
    /// Anthropic API key (starts with sk-ant-...)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Claude model to use. Default: claude-sonnet-4-5
    /// </summary>
    public string Model { get; set; } = "claude-sonnet-4-5";

    /// <summary>
    /// Maximum tokens for Claude responses. Default: 4096
    /// </summary>
    public int MaxTokens { get; set; } = 4096;
}
