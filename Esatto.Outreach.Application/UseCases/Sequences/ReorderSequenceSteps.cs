using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class ReorderSequenceSteps
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccess _access;

    public ReorderSequenceSteps(ISequenceRepository repo, SequenceAccess access)
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
