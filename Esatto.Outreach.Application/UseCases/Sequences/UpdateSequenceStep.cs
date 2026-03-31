using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class UpdateSequenceStep
{
    private readonly ISequenceRepository _repo;

    public UpdateSequenceStep(ISequenceRepository repo)
    {
        _repo = repo;
    }

    public async Task<SequenceStepViewDto> Handle(Guid sequenceId, Guid stepId, UpdateSequenceStepRequest request, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdWithDetailsAsync(sequenceId, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to modify this sequence");

        if (sequence.Status != SequenceStatus.Draft)
            throw new InvalidOperationException("You can only modify steps when the sequence is in Draft status.");

        var step = sequence.SequenceSteps.FirstOrDefault(s => s.Id == stepId);
        if (step == null)
            throw new KeyNotFoundException("Step not found in this sequence");

        step.UpdateDetails(
            request.StepType,
            request.DelayInDays,
            request.TimeOfDayToRun,
            request.GenerationType
        );

        await _repo.UpdateAsync(sequence, ct);
        return SequenceStepViewDto.FromEntity(step);
    }
}
