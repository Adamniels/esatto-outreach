using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.Shared;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Features.Prospects.CreateProspect;

public class CreateProspectCommandHandler
{
    private readonly IProspectRepository _repo;
    public CreateProspectCommandHandler(IProspectRepository repo) => _repo = repo;

    public async Task<ProspectViewDto> Handle(CreateProspectRequest request, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required");

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required");

        var entity = Prospect.CreateManual(
            name: request.Name,
            ownerId: userId,
            websiteUrls: request.Websites,
            notes: request.Notes
        );

        var saved = await _repo.AddAsync(entity, ct);
        return ProspectViewDto.FromEntity(saved);
    }
}
