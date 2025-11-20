using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.UseCases.Prospects;

public class ListProspects
{
    private readonly IProspectRepository _repo;
    public ListProspects(IProspectRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ProspectViewDto>> Handle(string userId, CancellationToken ct = default)
    {
        var list = await _repo.ListByOwnerAsync(userId, ct);
        return list.Select(ProspectViewDto.FromEntity).ToList();
    }
}
