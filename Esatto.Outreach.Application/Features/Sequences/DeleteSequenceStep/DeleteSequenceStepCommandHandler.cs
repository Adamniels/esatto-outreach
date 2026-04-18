using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.DeleteSequenceStep;

public class DeleteSequenceStepCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccessCommandHandler _access;

    public DeleteSequenceStepCommandHandler(ISequenceRepository repo, SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task Handle(Guid sequenceId, Guid stepId, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(sequenceId, userId, ct);
        sequence.RemoveStepOrThrow(stepId);
        await _repo.UpdateAsync(sequence, ct);
    }
}
