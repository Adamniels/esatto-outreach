using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.UseCases;

public class ListProspects
{
    private readonly IProspectRepository _repo;
    public ListProspects(IProspectRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ProspectViewDto>> Handle(CancellationToken ct = default)
    {
        var list = await _repo.ListAsync(ct);
        return list.Select(ProspectViewDto.FromEntity).ToList();
    }
}
