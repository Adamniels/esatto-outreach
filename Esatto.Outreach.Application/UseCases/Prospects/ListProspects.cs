using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Abstractions.Clients;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.DTOs.Prospects;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Application.DTOs.Intelligence;
using Esatto.Outreach.Application.DTOs.Outreach;
using Esatto.Outreach.Application.DTOs.Webhooks;
using Esatto.Outreach.Application.DTOs.Workflows;

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
