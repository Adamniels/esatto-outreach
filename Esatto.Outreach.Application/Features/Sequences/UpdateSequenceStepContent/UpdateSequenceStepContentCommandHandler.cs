using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.UpdateSequenceStepContent;

public class UpdateSequenceStepContentCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccessCommandHandler _access;

    public UpdateSequenceStepContentCommandHandler(ISequenceRepository repo, SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task<SequenceStepViewDto> Handle(Guid sequenceId, Guid stepId, UpdateSequenceStepContentRequest request, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(sequenceId, userId, ct);

        sequence.UpdateStepGeneratedContent(stepId, request.GeneratedSubject, request.GeneratedBody);

        await _repo.UpdateAsync(sequence, ct);

        var step = sequence.SequenceSteps.First(s => s.Id == stepId);
        return SequenceStepViewDto.FromEntity(step);
    }
}
