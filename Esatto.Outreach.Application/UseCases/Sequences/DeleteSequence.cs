using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class DeleteSequence
{
    private readonly ISequenceRepository _repo;

    public DeleteSequence(ISequenceRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(Guid id, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdAsync(id, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to delete this sequence");

        if (sequence.Status != SequenceStatus.Draft)
            throw new InvalidOperationException("Only draft sequences can be deleted. Please cancel or archive active sequences instead.");

        await _repo.DeleteAsync(sequence, ct);
    }
}
