using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.UpdateSequenceStepContent;

public class UpdateSequenceStepContentCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SequenceAccessCommandHandler _access;

    public UpdateSequenceStepContentCommandHandler(ISequenceRepository repo, IUnitOfWork unitOfWork, SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _access = access;
    }

    public async Task<SequenceStepViewDto> Handle(UpdateSequenceStepContentCommand command, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(command.SequenceId, userId, ct);

        sequence.UpdateStepGeneratedContent(command.StepId, command.GeneratedSubject, command.GeneratedBody);

        await _repo.UpdateAsync(sequence, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var step = sequence.SequenceSteps.First(s => s.Id == command.StepId);
        return SequenceStepViewDto.FromEntity(step);
    }
}
