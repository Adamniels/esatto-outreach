using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class AddSequenceStep
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccess _access;

    public AddSequenceStep(ISequenceRepository repo, SequenceAccess access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task<SequenceStepViewDto> Handle(Guid sequenceId, AddSequenceStepRequest request, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(sequenceId, userId, ct);

        var step = sequence.AddNewStep(
            request.StepType,
            request.DelayInDays,
            request.TimeOfDayToRun,
            request.GenerationType);

        await _repo.AddStepAsync(step, ct);
        return SequenceStepViewDto.FromEntity(step);
    }
}
