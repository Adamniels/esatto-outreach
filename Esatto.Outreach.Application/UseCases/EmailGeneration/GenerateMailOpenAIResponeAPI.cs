using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.UseCases.EmailGeneration;
public class GenerateMailOpenAIResponeAPI
{
    private readonly IEmailContextBuilder _contextBuilder;
    private readonly IEmailGeneratorFactory _generatorFactory;
    private readonly IProspectRepository _prospectRepository;

    public GenerateMailOpenAIResponeAPI(
        IEmailContextBuilder contextBuilder, 
        IEmailGeneratorFactory generatorFactory,
        IProspectRepository prospectRepository)
    {
        _contextBuilder = contextBuilder;
        _generatorFactory = generatorFactory;
        _prospectRepository = prospectRepository;
    }

    public async Task<ProspectViewDto> Handle(Guid id, string userId, string? type = null, CancellationToken ct = default)
    {
        // 1. Determine if we need soft data based on generator type
        bool includeSoftData = !string.IsNullOrWhiteSpace(type) && 
            type.Equals("UseCollectedData", StringComparison.OrdinalIgnoreCase);

        // 2. Build context with all required data
        var context = await _contextBuilder.BuildContextAsync(id, userId, includeSoftData, ct);

        // 3. Get the appropriate generator
        var generator = string.IsNullOrWhiteSpace(type) 
            ? _generatorFactory.GetGenerator() 
            : _generatorFactory.GetGenerator(type);

        // 4. Generate email draft using context
        var draft = await generator.GenerateAsync(context, ct);

        // 5. Save draft to prospect (business logic)
        var prospect = await _prospectRepository.GetByIdAsync(id, ct);
        if (prospect == null)
            throw new InvalidOperationException($"Prospect with id {id} not found");

        prospect.UpdateBasics(
            mailTitle: draft.Title,
            mailBodyPlain: draft.BodyPlain,
            mailBodyHTML: draft.BodyHTML
        );

        await _prospectRepository.UpdateAsync(prospect, ct);

        // 6. Return updated prospect view
        return ProspectViewDto.FromEntity(prospect);
    }
}
