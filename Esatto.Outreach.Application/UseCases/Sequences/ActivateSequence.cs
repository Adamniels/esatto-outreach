using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Entities.SequenceFeature;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class ActivateSequence
{
    private readonly ISequenceRepository _repo;

    public ActivateSequence(ISequenceRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(Guid sequenceId, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdWithDetailsAsync(sequenceId, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to modify this sequence");

        if (sequence.Status != SequenceStatus.Draft && sequence.Status != SequenceStatus.Paused)
            throw new InvalidOperationException("Only draft or paused sequences can be activated.");

        if (sequence.SequenceSteps.Count == 0)
            throw new InvalidOperationException("Cannot activate a sequence without steps.");

        if (sequence.SequenceSteps.Any(s => string.IsNullOrWhiteSpace(s.GeneratedBody)))
            throw new InvalidOperationException("All steps must have generated content before activation.");

        if (sequence.SequenceProspects.Count == 0)
            throw new InvalidOperationException("Cannot activate a sequence with no enclosed prospects.");

        if (sequence.Mode == SequenceMode.Focused && sequence.SequenceProspects.Count != 1)
            throw new InvalidOperationException("Focused sequence must contain exactly one prospect.");

        sequence.SetStatus(SequenceStatus.Active);

        foreach (var prospect in sequence.SequenceProspects.Where(p => p.Status == SequenceProspectStatus.Pending))
        {
            // Activate the prospect immediately to run the first step
            prospect.Activate(DateTime.UtcNow);
        }

        await _repo.UpdateAsync(sequence, ct);
    }
}
