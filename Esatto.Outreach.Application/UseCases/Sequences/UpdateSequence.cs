using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class UpdateSequence
{
    private readonly ISequenceRepository _repo;

    public UpdateSequence(ISequenceRepository repo)
    {
        _repo = repo;
    }

    public async Task<SequenceViewDto> Handle(Guid id, UpdateSequenceRequest request, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdAsync(id, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to modify this sequence");

        sequence.UpdateDetails(request.Title, request.Description);

        if (request.Settings != null)
        {
            if (sequence.Mode == Esatto.Outreach.Domain.Enums.SequenceMode.Focused)
            {
                sequence.Settings.UpdateFocusedSettings(
                    request.Settings.EnrichCompany ?? true,
                    request.Settings.EnrichContact ?? true);
            }
            else
            {
                sequence.Settings.UpdateMultiSettings(
                    request.Settings.ResearchSimilarities ?? false,
                    request.Settings.MaxActiveProspectsPerDay ?? 20);
            }
        }

        await _repo.UpdateAsync(sequence, ct);
        return SequenceViewDto.FromEntity(sequence);
    }
}
