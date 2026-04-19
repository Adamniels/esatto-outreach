using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Prospects.Shared;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.OutreachGeneration.GenerateLinkedInDraft;

public class GenerateLinkedInMessageCommandHandler
{
    private readonly IColdOutreachContextBuilder _contextBuilder;
    private readonly IColdOutreachGeneratorFactory _generatorFactory;
    private readonly IProspectRepository _prospectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateLinkedInMessageCommandHandler(
        IColdOutreachContextBuilder contextBuilder,
        IColdOutreachGeneratorFactory generatorFactory,
        IProspectRepository prospectRepository,
        IUnitOfWork unitOfWork)
    {
        _contextBuilder = contextBuilder;
        _generatorFactory = generatorFactory;
        _prospectRepository = prospectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProspectViewDto> Handle(GenerateLinkedInMessageCommand command, string userId, CancellationToken ct = default)
    {
        bool includeSoftData = !string.IsNullOrWhiteSpace(command.Type) &&
            command.Type.Equals(nameof(OutreachGenerationType.UseCollectedData), StringComparison.OrdinalIgnoreCase);

        var context = await _contextBuilder.BuildAsync(command.Id, userId, OutreachChannel.LinkedIn, includeSoftData, ct);

        OutreachGenerationType? generationType = string.IsNullOrWhiteSpace(command.Type)
            ? null
            : Enum.TryParse<OutreachGenerationType>(command.Type, ignoreCase: true, out var parsed) ? parsed : null;

        var generator = _generatorFactory.GetGenerator(generationType);
        var draft = await generator.GenerateAsync(context, ct);

        var prospect = await _prospectRepository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Prospect {command.Id} not found");

        prospect.UpdateLinkedInMessage(draft.BodyPlain);
        await _prospectRepository.UpdateAsync(prospect, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return ProspectViewDto.FromEntity(prospect);
    }
}
