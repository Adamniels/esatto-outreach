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
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.UseCases.Prospects;

public class CreateProspect
{
    private readonly IProspectRepository _repo;
    public CreateProspect(IProspectRepository repo) => _repo = repo;

    public async Task<ProspectViewDto> Handle(ProspectCreateDto dto, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Name is required");

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required");

        var entity = Prospect.CreateManual(
            name: dto.Name,
            ownerId: userId,
            websiteUrls: dto.Websites,
            notes: dto.Notes
        );

        var saved = await _repo.AddAsync(entity, ct);
        return ProspectViewDto.FromEntity(saved);
    }
}
