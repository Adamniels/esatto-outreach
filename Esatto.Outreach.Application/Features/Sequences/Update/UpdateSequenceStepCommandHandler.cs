using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences;

namespace Esatto.Outreach.Application.Features.Sequences;

public class UpdateSequenceStepCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccessCommandHandler _access;

    public UpdateSequenceStepCommandHandler(ISequenceRepository repo, SequenceAccessCommandHandler access)
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
