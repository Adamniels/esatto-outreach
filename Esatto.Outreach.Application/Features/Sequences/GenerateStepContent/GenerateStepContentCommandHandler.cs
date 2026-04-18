using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.GenerateStepContent;

public class GenerateStepContentCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly IOutreachContextBuilder _contextBuilder;
    private readonly IOutreachGeneratorFactory _generatorFactory;
    private readonly SequenceAccessCommandHandler _access;

    public GenerateStepContentCommandHandler(
        ISequenceRepository repo,
        IOutreachContextBuilder contextBuilder,
        IOutreachGeneratorFactory generatorFactory,
        SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _contextBuilder = contextBuilder;
        _generatorFactory = generatorFactory;
        _access = access;
    }

    public async Task<SequenceStepViewDto> Handle(GenerateStepContentCommand command, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(command.SequenceId, userId, ct);

        var step = sequence.GetMutableStep(command.StepId);
        var prospectId = sequence.GetBaselineProspectIdForContentGeneration();

        string? generatorType = step.GenerationType?.ToString();
        var generator = string.IsNullOrWhiteSpace(generatorType)
            ? _generatorFactory.GetGenerator()
            : _generatorFactory.GetGenerator(generatorType);

        var channel = step.GetOutreachChannel();
        var includeSoftData = sequence.IncludeCollectedDataForStepGeneration(step);

        var context = await _contextBuilder.BuildContextAsync(prospectId, userId, channel, includeSoftData, ct);
        var draft = await generator.GenerateAsync(context, ct);

        var body = string.IsNullOrWhiteSpace(draft.BodyHTML)
            ? (draft.BodyPlain ?? string.Empty)
            : draft.BodyHTML!;
        sequence.ApplyGeneratedContentFromDraft(command.StepId, draft.Title, body);

        await _repo.UpdateAsync(sequence, ct);

        var updated = sequence.SequenceSteps.First(s => s.Id == command.StepId);
        return SequenceStepViewDto.FromEntity(updated);
    }
}
