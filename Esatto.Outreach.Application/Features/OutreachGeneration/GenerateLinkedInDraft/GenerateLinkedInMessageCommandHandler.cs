using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Prospects.Shared;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.OutreachGeneration.GenerateLinkedInDraft;

public class GenerateLinkedInMessageCommandHandler
{
    private readonly IOutreachContextBuilder _contextBuilder;
    private readonly IOutreachGeneratorFactory _generatorFactory;
    private readonly IProspectRepository _prospectRepository;

    public GenerateLinkedInMessageCommandHandler(
        IOutreachContextBuilder contextBuilder,
        IOutreachGeneratorFactory generatorFactory,
        IProspectRepository prospectRepository)
    {
        _contextBuilder = contextBuilder;
        _generatorFactory = generatorFactory;
        _prospectRepository = prospectRepository;
    }

    public async Task<ProspectViewDto> Handle(GenerateLinkedInMessageCommand command, string userId, CancellationToken ct = default)
    {
        bool includeSoftData = !string.IsNullOrWhiteSpace(command.Type) &&
            (command.Type.Equals(nameof(OutreachGenerationType.UseCollectedData), StringComparison.OrdinalIgnoreCase));

        var context = await _contextBuilder.BuildContextAsync(command.Id, userId, OutreachChannel.LinkedIn, includeSoftData, ct);

        var generator = string.IsNullOrWhiteSpace(command.Type)
            ? _generatorFactory.GetGenerator()
            : _generatorFactory.GetGenerator(command.Type);

        var draft = await generator.GenerateAsync(context, ct);

        var prospect = await _prospectRepository.GetByIdAsync(command.Id, ct);
        if (prospect == null)
            throw new InvalidOperationException($"Prospect with id {command.Id} not found");

        prospect.UpdateLinkedInMessage(draft.BodyPlain);
        await _prospectRepository.UpdateAsync(prospect, ct);
        return ProspectViewDto.FromEntity(prospect);
    }
}
