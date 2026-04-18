using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.Shared;

namespace Esatto.Outreach.Application.Features.Prospects.ListProspects;

public class ListProspectsQueryHandler
{
    private readonly IProspectRepository _repo;
    public ListProspectsQueryHandler(IProspectRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ProspectViewDto>> Handle(ListProspectsQuery query, string userId, CancellationToken ct = default)
    {
        var list = await _repo.ListByOwnerAsync(userId, ct);
        return list.Select(ProspectViewDto.FromEntity).ToList();
    }
}
