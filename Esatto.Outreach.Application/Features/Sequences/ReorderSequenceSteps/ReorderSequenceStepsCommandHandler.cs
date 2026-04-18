using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.ReorderSequenceSteps;

public class ReorderSequenceStepsCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SequenceAccessCommandHandler _access;

    public ReorderSequenceStepsCommandHandler(ISequenceRepository repo, IUnitOfWork unitOfWork, SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _access = access;
    }

    public async Task Handle(ReorderSequenceStepsCommand command, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(command.SequenceId, userId, ct);
        sequence.ReorderSteps(command.StepIdsInOrder);
        await _repo.UpdateAsync(sequence, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
