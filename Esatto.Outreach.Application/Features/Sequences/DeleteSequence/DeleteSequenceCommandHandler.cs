using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.DeleteSequence;

public class DeleteSequenceCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccessCommandHandler _access;

    public DeleteSequenceCommandHandler(ISequenceRepository repo, SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task Handle(DeleteSequenceCommand command, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedAsync(command.Id, userId, ct);
        sequence.EnsureCanDelete();

        await _repo.DeleteAsync(sequence, ct);
    }
}
