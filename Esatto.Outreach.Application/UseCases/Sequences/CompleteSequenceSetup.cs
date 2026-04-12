using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class CompleteSequenceSetup
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccess _access;

    public CompleteSequenceSetup(ISequenceRepository repo, SequenceAccess access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task<SequenceViewDto> Handle(Guid id, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(id, userId, ct);
        sequence.CompleteWizard();
        await _repo.UpdateAsync(sequence, ct);
        return SequenceViewDto.FromEntity(sequence);
    }
}
