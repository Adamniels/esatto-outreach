using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.OutreachGeneration.Shared;
using Esatto.Outreach.Application.Features.Sequences.Shared;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.Sequences.GenerateStepContent;

public class GenerateStepContentCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFocusedSequenceStepContextBuilder _focusedContextBuilder;
    private readonly IFocusedSequenceStepGenerator _focusedGenerator;
    private readonly SequenceAccessCommandHandler _access;

    public GenerateStepContentCommandHandler(
        ISequenceRepository repo,
        IUnitOfWork unitOfWork,
        IFocusedSequenceStepContextBuilder focusedContextBuilder,
        IFocusedSequenceStepGenerator focusedGenerator,
        SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _focusedContextBuilder = focusedContextBuilder;
        _focusedGenerator = focusedGenerator;
        _access = access;
    }

    public async Task<SequenceStepViewDto> Handle(GenerateStepContentCommand command, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(command.SequenceId, userId, ct);

        if (sequence.Mode != SequenceMode.Focused)
            throw new InvalidOperationException("Per-step generation for multi-mode sequences is not yet implemented.");

        var orderedSteps = sequence.SequenceSteps.OrderBy(s => s.OrderIndex).ToList();
        var currentStep = orderedSteps.FirstOrDefault(s => s.Id == command.StepId)
            ?? throw new KeyNotFoundException("Step not found in this sequence");

        var stepNumber = orderedSteps.IndexOf(currentStep) + 1; // 1-based

        // Collect all steps before this one that already have generated content
        var priorTurns = orderedSteps
            .Take(stepNumber - 1)
            .Where(s => !string.IsNullOrWhiteSpace(s.GeneratedBody))
            .Select((s, i) => new PriorTurn(
                StepNumber: i + 1,
                StepType: s.StepType,
                DelayInDays: s.DelayInDays,
                Subject: s.GeneratedSubject,
                Body: s.GeneratedBody!))
            .ToList();

        var prospectId = sequence.GetBaselineProspectIdForContentGeneration();
        var strategy = currentStep.GenerationType ?? OutreachGenerationType.WebSearch;
        var channel = currentStep.GetOutreachChannel();

        var context = await _focusedContextBuilder.BuildAsync(
            prospectId,
            userId,
            channel,
            strategy,
            stepNumber,
            orderedSteps.Count,
            currentStep.StepType,
            currentStep.DelayInDays,
            priorTurns,
            ct);

        var draft = await _focusedGenerator.GenerateAsync(context, ct);

        var body = string.IsNullOrWhiteSpace(draft.BodyHTML) ? draft.BodyPlain : draft.BodyHTML!;
        sequence.ApplyGeneratedContentFromDraft(command.StepId, draft.Title, body);

        await _repo.UpdateAsync(sequence, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var updated = sequence.SequenceSteps.First(s => s.Id == command.StepId);
        return SequenceStepViewDto.FromEntity(updated);
    }
}
