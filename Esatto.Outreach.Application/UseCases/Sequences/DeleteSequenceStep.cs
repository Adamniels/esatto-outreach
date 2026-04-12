using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class DeleteSequenceStep
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccess _access;

    public DeleteSequenceStep(ISequenceRepository repo, SequenceAccess access)
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
