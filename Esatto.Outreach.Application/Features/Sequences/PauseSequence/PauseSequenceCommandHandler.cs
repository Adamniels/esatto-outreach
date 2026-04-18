using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.PauseSequence;

public class PauseSequenceCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccessCommandHandler _access;

    public PauseSequenceCommandHandler(ISequenceRepository repo, SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task Handle(PauseSequenceCommand command, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedAsync(command.SequenceId, userId, ct);
        sequence.Pause();
        await _repo.UpdateAsync(sequence, ct);
    }
}
