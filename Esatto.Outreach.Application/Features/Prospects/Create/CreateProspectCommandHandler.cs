using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Features.Prospects;

public class CreateProspectCommandHandler
{
    private readonly IProspectRepository _repo;
    public CreateProspectCommandHandler(IProspectRepository repo) => _repo = repo;

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
