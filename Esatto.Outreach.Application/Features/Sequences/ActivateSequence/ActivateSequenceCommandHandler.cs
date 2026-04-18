using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.ActivateSequence;

public class ActivateSequenceCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccessCommandHandler _access;

    public ActivateSequenceCommandHandler(ISequenceRepository repo, SequenceAccessCommandHandler access)
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
