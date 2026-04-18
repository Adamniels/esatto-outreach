using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.CompleteSequenceSetup;

public class CompleteSequenceSetupCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccessCommandHandler _access;

    public CompleteSequenceSetupCommandHandler(ISequenceRepository repo, SequenceAccessCommandHandler access)
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
