using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class GenerateStepContent
{
    private readonly ISequenceRepository _repo;
    private readonly IOutreachContextBuilder _contextBuilder;
    private readonly IOutreachGeneratorFactory _generatorFactory;
    private readonly SequenceAccess _access;

    public GenerateStepContent(
        ISequenceRepository repo,
        IOutreachContextBuilder contextBuilder,
        IOutreachGeneratorFactory generatorFactory,
        SequenceAccess access)
    {
        _repo = repo;
        _contextBuilder = contextBuilder;
        _generatorFactory = generatorFactory;
        _access = access;
    }

    // TODO: Here I want different things depending on if it is a focused or multi sequence. So maybe to new functions depending on it?
    // Multi want a way to get some information of all the prospect and focused need the prospect enrichment if the setting is set
    public async Task<SequenceStepViewDto> Handle(Guid sequenceId, Guid stepId, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(sequenceId, userId, ct);

        var step = sequence.GetMutableStep(stepId);
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
        sequence.ApplyGeneratedContentFromDraft(stepId, draft.Title, body);

        await _repo.UpdateAsync(sequence, ct);

        var updated = sequence.SequenceSteps.First(s => s.Id == stepId);
        return SequenceStepViewDto.FromEntity(updated);
    }
}
