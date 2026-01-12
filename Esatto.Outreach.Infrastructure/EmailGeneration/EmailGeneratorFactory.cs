using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Infrastructure.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Esatto.Outreach.Infrastructure.EmailGeneration;

/// <summary>
/// Factory implementation for creating the appropriate email generator based on configuration.
/// </summary>
public sealed class EmailGeneratorFactory : IEmailGeneratorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EmailGenerationOptions _options;

    public EmailGeneratorFactory(
        IServiceProvider serviceProvider,
        IOptions<EmailGenerationOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public ICustomEmailGenerator GetGenerator()
    {
        return _options.DefaultType switch
        {
            EmailGenerationType.WebSearch => _serviceProvider.GetRequiredService<OpenAICustomEmailGenerator>(),
            EmailGenerationType.UseCollectedData => _serviceProvider.GetRequiredService<CollectedDataEmailGenerator>(),
            EmailGenerationType.EsattoRag => _serviceProvider.GetRequiredService<EsattoRagEmailGenerator>(),
            _ => throw new InvalidOperationException($"Unknown generator type: {_options.DefaultType}")
        };
    }

    public ICustomEmailGenerator GetGenerator(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return GetGenerator();

        if (!Enum.TryParse<EmailGenerationType>(type, ignoreCase: true, out var generatorType))
        {
            throw new ArgumentException(
                $"Invalid generator type: '{type}'. Valid values are: WebSearch, UseCollectedData",
                nameof(type));
        }

        return generatorType switch
        {
            EmailGenerationType.WebSearch => _serviceProvider.GetRequiredService<OpenAICustomEmailGenerator>(),
            EmailGenerationType.UseCollectedData => _serviceProvider.GetRequiredService<CollectedDataEmailGenerator>(),
            EmailGenerationType.EsattoRag => _serviceProvider.GetRequiredService<EsattoRagEmailGenerator>(),
            _ => throw new InvalidOperationException($"Unknown generator type: {generatorType}")
        };
    }
}
