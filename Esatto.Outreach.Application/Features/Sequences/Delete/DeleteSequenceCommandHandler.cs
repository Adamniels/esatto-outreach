using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.Sequences;

public class DeleteSequenceCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccessCommandHandler _access;

    public DeleteSequenceCommandHandler(ISequenceRepository repo, SequenceAccessCommandHandler access)
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
