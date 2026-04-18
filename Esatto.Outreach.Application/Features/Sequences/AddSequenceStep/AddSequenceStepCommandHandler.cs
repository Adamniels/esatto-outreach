using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.AddSequenceStep;

public class AddSequenceStepCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SequenceAccessCommandHandler _access;

    public AddSequenceStepCommandHandler(ISequenceRepository repo, IUnitOfWork unitOfWork, SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _access = access;
    }

    public async Task<SequenceStepViewDto> Handle(AddSequenceStepCommand command, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(command.SequenceId, userId, ct);

        var step = sequence.AddNewStep(
            command.StepType,
            command.DelayInDays,
            command.TimeOfDayToRun,
            command.GenerationType);

        await _repo.AddStepAsync(step, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return SequenceStepViewDto.FromEntity(step);
    }
}
