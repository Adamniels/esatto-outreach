using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;
using Esatto.Outreach.Domain.Entities.SequenceFeature;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class AddSequenceStep
{
    private readonly ISequenceRepository _repo;

    public AddSequenceStep(ISequenceRepository repo)
    {
        _repo = repo;
    }

    public async Task<SequenceStepViewDto> Handle(Guid sequenceId, AddSequenceStepRequest request, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdWithDetailsAsync(sequenceId, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to modify this sequence");

        int nextOrderIndex = sequence.SequenceSteps.Count > 0 
            ? sequence.SequenceSteps.Max(s => s.OrderIndex) + 1 
            : 0;

        var step = SequenceStep.Create(
            sequenceId,
            nextOrderIndex,
            request.StepType,
            request.DelayInDays,
            request.TimeOfDayToRun,
            request.GenerationType
        );

        sequence.AddStep(step);

        await _repo.AddStepAsync(step, ct);
        return SequenceStepViewDto.FromEntity(step);
    }
}
