using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.DTOs.Sequence;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class GenerateStepContent
{
    private readonly ISequenceRepository _repo;
    private readonly IOutreachContextBuilder _contextBuilder;
    private readonly IOutreachGeneratorFactory _generatorFactory;

    public GenerateStepContent(
        ISequenceRepository repo,
        IOutreachContextBuilder contextBuilder,
        IOutreachGeneratorFactory generatorFactory)
    {
        _repo = repo;
        _contextBuilder = contextBuilder;
        _generatorFactory = generatorFactory;
    }

    public async Task<SequenceStepViewDto> Handle(Guid sequenceId, Guid stepId, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdWithDetailsAsync(sequenceId, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to modify this sequence");

        var step = sequence.SequenceSteps.FirstOrDefault(s => s.Id == stepId);
        if (step == null)
            throw new KeyNotFoundException("Step not found in this sequence");

        // Use standard generator type, or specific based on step settings
        string? generatorType = step.GenerationType?.ToString();
        var generator = string.IsNullOrWhiteSpace(generatorType)
            ? _generatorFactory.GetGenerator()
            : _generatorFactory.GetGenerator(generatorType);

        OutreachChannel channel = step.StepType switch
        {
            SequenceStepType.Email => OutreachChannel.Email,
            SequenceStepType.LinkedInMessage => OutreachChannel.LinkedIn,
            SequenceStepType.LinkedInConnectionRequest => OutreachChannel.LinkedIn,
            SequenceStepType.LinkedInInteraction => OutreachChannel.LinkedIn,
            _ => OutreachChannel.Email
        };

        if (sequence.Mode == SequenceMode.Focused)
        {
            var prospect = sequence.SequenceProspects.FirstOrDefault()?.ProspectId;
            if (prospect == null)
                throw new InvalidOperationException("Focused sequence must have exactly one prospect enrolled to generate content.");

            // Enrichment check is simplified here for now. A real enrichment step might be called before context building.
            // TODO: Implement a proper enrichment status check and possibly trigger enrichment before generation if data is stale or missing.
            var includeSoftData = step.GenerationType == OutreachGenerationType.UseCollectedData;

            var context = await _contextBuilder.BuildContextAsync(prospect.Value, userId, channel, includeSoftData, ct);
            var draft = await generator.GenerateAsync(context, ct);

            step.SetGeneratedContent(draft.Title, string.IsNullOrWhiteSpace(draft.BodyHTML) ? draft.BodyPlain : draft.BodyHTML);
        }
        else
        {
            // For multi-mode, logic implies AI finding commonalities. Right now, we might not have a generic multi-context builder.
            // Simplified: use the first enrolled prospect's context as baseline for multi (or stub it).
            // A real IMultiOutreachContextBuilder might be needed in the future.
            // TODO: Implement a proper multi-prospect context builder that can synthesize commonalities across prospects for better multi-step generation.
            var representativeProspect = sequence.SequenceProspects.FirstOrDefault()?.ProspectId;
            if (representativeProspect == null)
                throw new InvalidOperationException("Multi sequence must have at least one prospect enrolled to establish a prompt baseline.");

            var context = await _contextBuilder.BuildContextAsync(representativeProspect.Value, userId, channel, false, ct);
            var draft = await generator.GenerateAsync(context, ct);

            step.SetGeneratedContent(draft.Title, string.IsNullOrWhiteSpace(draft.BodyHTML) ? draft.BodyPlain : draft.BodyHTML);
        }

        await _repo.UpdateAsync(sequence, ct);
        return SequenceStepViewDto.FromEntity(step);
    }
}
