using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class SaveBuilderProgress
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccess _access;

    public SaveBuilderProgress(ISequenceRepository repo, SequenceAccess access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task<SequenceViewDto> Handle(Guid id, SaveBuilderProgressRequest request, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedAsync(id, userId, ct);

        sequence.UpdateBuilderStep(request.CurrentBuilderStep);

        await _repo.UpdateAsync(sequence, ct);
        return SequenceViewDto.FromEntity(sequence);
    }
}
