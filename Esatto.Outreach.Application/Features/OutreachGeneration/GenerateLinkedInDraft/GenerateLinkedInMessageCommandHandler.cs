using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Prospects.Shared;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.OutreachGeneration.GenerateLinkedInDraft;

public class GenerateLinkedInMessageCommandHandler
{
    private readonly IColdOutreachContextBuilder _contextBuilder;
    private readonly IColdOutreachGenerator _generator;
    private readonly IProspectRepository _prospectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateLinkedInMessageCommandHandler(
        IColdOutreachContextBuilder contextBuilder,
        IColdOutreachGenerator generator,
        IProspectRepository prospectRepository,
        IUnitOfWork unitOfWork)
    {
        _contextBuilder = contextBuilder;
        _generator = generator;
        _prospectRepository = prospectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProspectViewDto> Handle(GenerateLinkedInMessageCommand command, string userId, CancellationToken ct = default)
    {
        var strategy = ParseStrategy(command.Type);
        var context = await _contextBuilder.BuildAsync(command.Id, userId, OutreachChannel.LinkedIn, strategy, ct);
        var draft = await _generator.GenerateAsync(context, ct);

        var prospect = await _prospectRepository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Prospect {command.Id} not found");

        prospect.UpdateLinkedInMessage(draft.BodyPlain);
        await _prospectRepository.UpdateAsync(prospect, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return ProspectViewDto.FromEntity(prospect);
    }

    private static OutreachGenerationType ParseStrategy(string? type) =>
        Enum.TryParse<OutreachGenerationType>(type, ignoreCase: true, out var parsed)
            ? parsed
            : OutreachGenerationType.WebSearch;
}
