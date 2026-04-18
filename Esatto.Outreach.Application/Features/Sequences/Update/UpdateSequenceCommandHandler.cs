using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences;

namespace Esatto.Outreach.Application.Features.Sequences;

public class UpdateSequenceCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly SequenceAccessCommandHandler _access;

    public UpdateSequenceCommandHandler(ISequenceRepository repo, SequenceAccessCommandHandler access)
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
