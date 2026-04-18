using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.CancelSequence;

public class CancelSequenceCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccessCommandHandler _access;

    public CancelSequenceCommandHandler(ISequenceRepository repo, SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task Handle(CancelSequenceCommand command, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedAsync(command.SequenceId, userId, ct);
        sequence.CancelToArchived();
        await _repo.UpdateAsync(sequence, ct);
    }
}
