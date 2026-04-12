using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class UpdateSequenceStepContent
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccess _access;

    public UpdateSequenceStepContent(ISequenceRepository repo, SequenceAccess access)
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
