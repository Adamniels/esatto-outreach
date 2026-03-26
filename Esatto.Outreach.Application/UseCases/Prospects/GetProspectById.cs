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

public class GetProspectById
{
    private readonly IProspectRepository _repo;
    public GetProspectById(IProspectRepository repo) => _repo = repo;

    public async Task<ProspectViewDto?> Handle(Guid id, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        return entity is null ? null : ProspectViewDto.FromEntity(entity);
    }
}
