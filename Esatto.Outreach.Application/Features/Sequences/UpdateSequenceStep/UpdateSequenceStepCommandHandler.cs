using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.UpdateSequenceStep;

public class UpdateSequenceStepCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccessCommandHandler _access;

    public UpdateSequenceStepCommandHandler(ISequenceRepository repo, SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task<SequenceStepViewDto> Handle(UpdateSequenceStepCommand command, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(command.SequenceId, userId, ct);

        sequence.UpdateStepDefinition(
            command.StepId,
            command.StepType,
            command.DelayInDays,
            command.TimeOfDayToRun,
            command.GenerationType);

        await _repo.UpdateAsync(sequence, ct);

        var step = sequence.SequenceSteps.First(s => s.Id == command.StepId);
        return SequenceStepViewDto.FromEntity(step);
    }
}
