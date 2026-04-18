using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.SaveBuilderProgress;

public class SaveBuilderProgressCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccessCommandHandler _access;

    public SaveBuilderProgressCommandHandler(ISequenceRepository repo, SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task<SequenceViewDto> Handle(SaveBuilderProgressCommand command, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedAsync(command.Id, userId, ct);

        sequence.UpdateBuilderStep(command.CurrentBuilderStep);

        await _repo.UpdateAsync(sequence, ct);
        return SequenceViewDto.FromEntity(sequence);
    }
}
