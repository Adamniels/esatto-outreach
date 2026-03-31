using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class ReorderSequenceSteps
{
    private readonly ISequenceRepository _repo;

    public ReorderSequenceSteps(ISequenceRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(Guid sequenceId, ReorderSequenceStepsRequest request, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdWithDetailsAsync(sequenceId, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to modify this sequence");

        if (sequence.Status != SequenceStatus.Draft)
            throw new InvalidOperationException("You can only reorder steps when the sequence is in Draft status.");

        var newOrderIds = request.StepIdsInOrder.Distinct().ToList();

        if (newOrderIds.Count != sequence.SequenceSteps.Count)
            throw new ArgumentException("The provided step IDs do not match the number of steps in the sequence.");

        for (int i = 0; i < newOrderIds.Count; i++)
        {
            var step = sequence.SequenceSteps.FirstOrDefault(s => s.Id == newOrderIds[i]);
            if (step == null)
                throw new KeyNotFoundException($"Step with ID {newOrderIds[i]} is not part of this sequence.");

            step.UpdateOrder(i);
        }

        await _repo.UpdateAsync(sequence, ct);
    }
}
