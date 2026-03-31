using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class CancelSequence
{
    private readonly ISequenceRepository _repo;

    public CancelSequence(ISequenceRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(Guid sequenceId, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdAsync(sequenceId, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to modify this sequence");

        if (sequence.Status != SequenceStatus.Active && sequence.Status != SequenceStatus.Paused)
            throw new InvalidOperationException("Only active or paused sequences can be canceled.");

        sequence.SetStatus(SequenceStatus.Archived);

        await _repo.UpdateAsync(sequence, ct);
    }
}
