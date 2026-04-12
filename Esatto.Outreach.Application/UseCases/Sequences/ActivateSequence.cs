using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class ActivateSequence
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccess _access;

    public ActivateSequence(ISequenceRepository repo, SequenceAccess access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task Handle(Guid sequenceId, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(sequenceId, userId, ct);
        sequence.Activate(DateTime.UtcNow);
        await _repo.UpdateAsync(sequence, ct);
    }
}
