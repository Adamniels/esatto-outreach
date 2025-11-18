using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Infrastructure.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Esatto.Outreach.Infrastructure.SoftDataCollection;

/// <summary>
/// Factory implementation for creating the appropriate research service based on configuration.
/// </summary>
public sealed class ResearchServiceFactory : IResearchServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SoftDataCollectionOptions _options;

    public ResearchServiceFactory(
        IServiceProvider serviceProvider,
        IOptions<SoftDataCollectionOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public IResearchService GetResearchService()
    {
        return _options.Provider switch
        {
            AiProviderType.OpenAI => _serviceProvider.GetRequiredService<OpenAIResearchService>(),
            AiProviderType.Claude => _serviceProvider.GetRequiredService<ClaudeResearchService>(),
            AiProviderType.Hybrid => CreateHybridService(),
            _ => throw new InvalidOperationException($"Unknown provider type: {_options.Provider}")
        };
    }

    public IResearchService GetResearchService(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            return GetResearchService();

        if (!Enum.TryParse<AiProviderType>(providerName, ignoreCase: true, out var providerType))
        {
            throw new ArgumentException(
                $"Invalid provider name: '{providerName}'. Valid values are: OpenAI, Claude, Hybrid",
                nameof(providerName));
        }

        return providerType switch
        {
            AiProviderType.OpenAI => _serviceProvider.GetRequiredService<OpenAIResearchService>(),
            AiProviderType.Claude => _serviceProvider.GetRequiredService<ClaudeResearchService>(),
            AiProviderType.Hybrid => CreateHybridService(),
            _ => throw new InvalidOperationException($"Unknown provider type: {providerType}")
        };
    }

    private HybridResearchService CreateHybridService()
    {
        var openAIService = _serviceProvider.GetRequiredService<OpenAIResearchService>();
        var claudeService = _serviceProvider.GetRequiredService<ClaudeResearchService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<HybridResearchService>>();
        
        return new HybridResearchService(openAIService, claudeService, logger);
    }
}
