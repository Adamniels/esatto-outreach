using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class RemoveProspect
{
    private readonly ISequenceRepository _repo;

    public RemoveProspect(ISequenceRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(Guid sequenceId, Guid sequenceProspectId, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdWithDetailsAsync(sequenceId, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to modify this sequence");

        var sp = sequence.SequenceProspects.FirstOrDefault(p => p.Id == sequenceProspectId);
        if (sp == null)
            throw new KeyNotFoundException("Prospect is not enrolled in this sequence");

        // We probably only allow removal if pending, or if we want to force stop them.
        // As a business rule, lets say if they are active, it aborts the execution for them.
        sequence.SequenceProspects.Remove(sp);

        await _repo.UpdateAsync(sequence, ct);
    }
}
