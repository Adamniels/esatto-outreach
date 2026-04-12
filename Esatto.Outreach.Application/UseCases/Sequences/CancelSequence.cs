using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class CancelSequence
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccess _access;

    public CancelSequence(ISequenceRepository repo, SequenceAccess access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task Handle(Guid sequenceId, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedAsync(sequenceId, userId, ct);
        sequence.CancelToArchived();
        await _repo.UpdateAsync(sequence, ct);
    }
}
