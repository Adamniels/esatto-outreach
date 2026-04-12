using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class DeleteSequence
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccess _access;

    public DeleteSequence(ISequenceRepository repo, SequenceAccess access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task Handle(Guid id, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedAsync(id, userId, ct);
        sequence.EnsureCanDelete();

        await _repo.DeleteAsync(sequence, ct);
    }
}
