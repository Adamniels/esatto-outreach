using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class UpdateSequenceStep
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccess _access;

    public UpdateSequenceStep(ISequenceRepository repo, SequenceAccess access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task<SequenceStepViewDto> Handle(Guid sequenceId, Guid stepId, UpdateSequenceStepRequest request, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(sequenceId, userId, ct);

        sequence.UpdateStepDefinition(
            stepId,
            request.StepType,
            request.DelayInDays,
            request.TimeOfDayToRun,
            request.GenerationType);

        await _repo.UpdateAsync(sequence, ct);

        var step = sequence.SequenceSteps.First(s => s.Id == stepId);
        return SequenceStepViewDto.FromEntity(step);
    }
}
