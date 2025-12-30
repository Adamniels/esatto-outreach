namespace Esatto.Outreach.Application.Abstractions;

/// <summary>
/// Factory for creating the appropriate research service based on configuration.
/// Implements Strategy Pattern for provider selection.
/// </summary>
public interface IResearchServiceFactory
{
    /// <summary>
    /// Gets the configured research service instance (OpenAI, Claude, or Hybrid).
    /// </summary>
    /// <returns>Research service implementation based on configuration</returns>
    IResearchService GetResearchService();

    /// <summary>
    /// Gets a specific research service by provider name, overriding configuration.
    /// </summary>
    /// <param name="providerName">Provider name: "OpenAI", "Claude", or "Hybrid"</param>
    /// <returns>Research service implementation for the specified provider</returns>
    IResearchService GetResearchService(string providerName);
}
