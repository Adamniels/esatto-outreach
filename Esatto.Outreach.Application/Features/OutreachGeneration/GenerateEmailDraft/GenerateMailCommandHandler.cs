using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Prospects.Shared;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.OutreachGeneration.GenerateEmailDraft;

public class GenerateMailCommandHandler
{
    private readonly IColdOutreachContextBuilder _contextBuilder;
    private readonly IColdOutreachGenerator _generator;
    private readonly IProspectRepository _prospectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateMailCommandHandler(
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

    public async Task<ProspectViewDto> Handle(GenerateMailCommand command, string userId, CancellationToken ct = default)
    {
        var strategy = ParseStrategy(command.Type);
        var context = await _contextBuilder.BuildAsync(command.Id, userId, OutreachChannel.Email, strategy, ct);
        var draft = await _generator.GenerateAsync(context, ct);

        var prospect = await _prospectRepository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Prospect {command.Id} not found");

        prospect.UpdateBasics(
            mailTitle: draft.Title,
            mailBodyPlain: draft.BodyPlain,
            mailBodyHTML: draft.BodyHTML);

        await _prospectRepository.UpdateAsync(prospect, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return ProspectViewDto.FromEntity(prospect);
    }

    private static OutreachGenerationType ParseStrategy(string? type) =>
        Enum.TryParse<OutreachGenerationType>(type, ignoreCase: true, out var parsed)
            ? parsed
            : OutreachGenerationType.WebSearch;
}
