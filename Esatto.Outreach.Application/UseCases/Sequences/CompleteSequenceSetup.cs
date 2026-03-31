using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class CompleteSequenceSetup
{
    private readonly ISequenceRepository _repo;

    public CompleteSequenceSetup(ISequenceRepository repo)
    {
        _repo = repo;
    }

    public async Task<SequenceViewDto> Handle(Guid id, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdWithDetailsAsync(id, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to modify this sequence");

        if (!sequence.SequenceSteps.Any())
            throw new InvalidOperationException("Sequence must have at least one step before setup can be completed");

        if (!sequence.SequenceProspects.Any())
            throw new InvalidOperationException("Sequence must have at least one prospect before setup can be completed");

        sequence.CompleteSetup();

        await _repo.UpdateAsync(sequence, ct);
        return SequenceViewDto.FromEntity(sequence);
    }
}
