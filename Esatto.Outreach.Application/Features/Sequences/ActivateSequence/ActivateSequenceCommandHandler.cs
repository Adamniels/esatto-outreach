using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.ActivateSequence;

public class ActivateSequenceCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SequenceAccessCommandHandler _access;

    public ActivateSequenceCommandHandler(ISequenceRepository repo, IUnitOfWork unitOfWork, SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _access = access;
    }

    public async Task Handle(ActivateSequenceCommand command, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(command.SequenceId, userId, ct);
        sequence.Activate(DateTime.UtcNow);
        await _repo.UpdateAsync(sequence, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
