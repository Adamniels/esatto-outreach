using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects;

namespace Esatto.Outreach.Application.Features.Prospects;

public class GetProspectByIdQueryHandler
{
    private readonly IProspectRepository _repo;
    public GetProspectByIdQueryHandler(IProspectRepository repo) => _repo = repo;

    public async Task<ProspectViewDto?> Handle(Guid id, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        return entity is null ? null : ProspectViewDto.FromEntity(entity);
    }
}
