using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class UpdateSequence
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccess _access;

    public UpdateSequence(ISequenceRepository repo, SequenceAccess access)
    {
        _repo = repo;
        _access = access;
    }

    public async Task<SequenceViewDto> Handle(Guid id, UpdateSequenceRequest request, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedAsync(id, userId, ct);

        sequence.ApplyMetadataUpdate(
            request.Title,
            request.Description,
            applySettings: request.Settings != null,
            enrichCompany: request.Settings?.EnrichCompany ?? true,
            enrichContact: request.Settings?.EnrichContact ?? true,
            researchSimilarities: request.Settings?.ResearchSimilarities ?? false,
            maxActiveProspectsPerDay: request.Settings?.MaxActiveProspectsPerDay ?? 20);

        await _repo.UpdateAsync(sequence, ct);
        return SequenceViewDto.FromEntity(sequence);
    }
}
