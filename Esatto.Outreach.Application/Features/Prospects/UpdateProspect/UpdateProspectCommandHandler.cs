using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.Shared;

namespace Esatto.Outreach.Application.Features.Prospects.UpdateProspect;

public class UpdateProspectCommandHandler
{
    private readonly IProspectRepository _repo;
    public UpdateProspectCommandHandler(IProspectRepository repo) => _repo = repo;

    public async Task<ProspectViewDto?> Handle(Guid id, UpdateProspectRequest request, string userId, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity is null) return null;

        if (entity.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to update this prospect");

        entity.UpdateBasics(
            name: request.Name,
            websiteUrls: request.Websites,
            notes: request.Notes,
            mailTitle: request.MailTitle,
            mailBodyPlain: request.MailBodyPlain,
            mailBodyHTML: request.MailBodyHtml,
            linkedInMessage: request.LinkedInMessage
        );

        if (request.Status.HasValue)
            entity.SetStatus(request.Status.Value);

        await _repo.UpdateAsync(entity, ct);
        return ProspectViewDto.FromEntity(entity);
    }
}
