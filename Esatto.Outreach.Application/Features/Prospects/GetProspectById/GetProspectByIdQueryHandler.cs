using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.Shared;

namespace Esatto.Outreach.Application.Features.Prospects.GetProspectById;

public class GetProspectByIdQueryHandler
{
    private readonly IProspectRepository _repo;
    public GetProspectByIdQueryHandler(IProspectRepository repo) => _repo = repo;

    public async Task<ProspectViewDto?> Handle(GetProspectByIdQuery query, string userId, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(query.Id, ct);
        if (entity is null) return null;

        if (entity.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to access this prospect");

        return ProspectViewDto.FromEntity(entity);
    }
}
