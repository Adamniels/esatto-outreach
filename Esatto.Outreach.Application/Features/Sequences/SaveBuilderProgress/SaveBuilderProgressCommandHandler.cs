using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.SaveBuilderProgress;

public class SaveBuilderProgressCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SequenceAccessCommandHandler _access;

    public SaveBuilderProgressCommandHandler(ISequenceRepository repo, IUnitOfWork unitOfWork, SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _access = access;
    }

    public async Task<SequenceViewDto> Handle(SaveBuilderProgressCommand command, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedAsync(command.Id, userId, ct);

        sequence.UpdateBuilderStep(command.CurrentBuilderStep);

        await _repo.UpdateAsync(sequence, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return SequenceViewDto.FromEntity(sequence);
    }
}
