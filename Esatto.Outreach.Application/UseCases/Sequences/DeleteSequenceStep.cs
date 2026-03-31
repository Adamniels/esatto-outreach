using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class DeleteSequenceStep
{
    private readonly ISequenceRepository _repo;

    public DeleteSequenceStep(ISequenceRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(Guid sequenceId, Guid stepId, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdWithDetailsAsync(sequenceId, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to modify this sequence");

        if (sequence.Status != SequenceStatus.Draft)
            throw new InvalidOperationException("You can only delete steps when the sequence is in Draft status.");

        var step = sequence.SequenceSteps.FirstOrDefault(s => s.Id == stepId);
        if (step == null)
            throw new KeyNotFoundException("Step not found in this sequence");

        sequence.SequenceSteps.Remove(step);

        // Reorder remaining steps
        var remainingSteps = sequence.SequenceSteps.OrderBy(s => s.OrderIndex).ToList();
        for (int i = 0; i < remainingSteps.Count; i++)
        {
            remainingSteps[i].UpdateOrder(i);
        }

        await _repo.UpdateAsync(sequence, ct);
    }
}
