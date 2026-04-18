using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Prospects.Shared;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.OutreachGeneration.GenerateEmailDraft;
public class GenerateMailCommandHandler
{
    private readonly IOutreachContextBuilder _contextBuilder;
    private readonly IOutreachGeneratorFactory _generatorFactory;
    private readonly IProspectRepository _prospectRepository;

    public GenerateMailCommandHandler(
        IOutreachContextBuilder contextBuilder,
        IOutreachGeneratorFactory generatorFactory,
        IProspectRepository prospectRepository)
    {
        _contextBuilder = contextBuilder;
        _generatorFactory = generatorFactory;
        _prospectRepository = prospectRepository;
    }

    public async Task<ProspectViewDto> Handle(Guid id, string userId, string? type = null, CancellationToken ct = default)
    {
        bool includeSoftData = !string.IsNullOrWhiteSpace(type) &&
            type.Equals(nameof(OutreachGenerationType.UseCollectedData), StringComparison.OrdinalIgnoreCase);

        var context = await _contextBuilder.BuildContextAsync(id, userId, OutreachChannel.Email, includeSoftData, ct);

        var generator = string.IsNullOrWhiteSpace(type)
            ? _generatorFactory.GetGenerator()
            : _generatorFactory.GetGenerator(type);

        var draft = await generator.GenerateAsync(context, ct);

        var prospect = await _prospectRepository.GetByIdAsync(id, ct);
        if (prospect == null)
            throw new InvalidOperationException($"Prospect with id {id} not found");

        prospect.UpdateBasics(
            mailTitle: draft.Title,
            mailBodyPlain: draft.BodyPlain,
            mailBodyHTML: draft.BodyHTML
        );

        await _prospectRepository.UpdateAsync(prospect, ct);
        return ProspectViewDto.FromEntity(prospect);
    }
}
