using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences;

namespace Esatto.Outreach.Application.Features.Sequences;

public class ReorderSequenceStepsCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccessCommandHandler _access;

    public ReorderSequenceStepsCommandHandler(ISequenceRepository repo, SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task Handle(Guid sequenceId, ReorderSequenceStepsRequest request, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(sequenceId, userId, ct);
        sequence.ReorderSteps(request.StepIdsInOrder);
        await _repo.UpdateAsync(sequence, ct);
    }
}
