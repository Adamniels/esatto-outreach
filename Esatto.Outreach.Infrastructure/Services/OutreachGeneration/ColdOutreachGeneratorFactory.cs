using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Application.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Esatto.Outreach.Infrastructure.Services.OutreachGeneration;

public sealed class ColdOutreachGeneratorFactory : IColdOutreachGeneratorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OutreachGenerationOptions _options;

    public ColdOutreachGeneratorFactory(
        IServiceProvider serviceProvider,
        IOptions<OutreachGenerationOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public IColdOutreachGenerator GetGenerator(OutreachGenerationType? type = null)
    {
        var resolved = type ?? _options.DefaultType;

        return resolved switch
        {
            OutreachGenerationType.WebSearch => _serviceProvider.GetRequiredService<OpenAIColdOutreachGenerator>(),
            OutreachGenerationType.UseCollectedData => _serviceProvider.GetRequiredService<CollectedDataColdOutreachGenerator>(),
            _ => throw new InvalidOperationException($"Unknown generator type: {resolved}")
        };
    }
}
