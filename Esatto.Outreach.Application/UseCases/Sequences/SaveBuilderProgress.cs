using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class SaveBuilderProgress
{
    private readonly ISequenceRepository _repo;

    public SaveBuilderProgress(ISequenceRepository repo)
    {
        _repo = repo;
    }

    public async Task<SequenceViewDto> Handle(Guid id, SaveBuilderProgressRequest request, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdAsync(id, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to modify this sequence");

        sequence.UpdateBuilderStep(request.CurrentBuilderStep);

        await _repo.UpdateAsync(sequence, ct);
        return SequenceViewDto.FromEntity(sequence);
    }
}
