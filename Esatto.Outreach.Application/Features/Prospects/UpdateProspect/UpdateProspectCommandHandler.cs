using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.Shared;

namespace Esatto.Outreach.Application.Features.Prospects.UpdateProspect;

public class UpdateProspectCommandHandler
{
    private readonly IProspectRepository _repo;
    public UpdateProspectCommandHandler(IProspectRepository repo) => _repo = repo;

    public async Task<ProspectViewDto?> Handle(UpdateProspectCommand command, string userId, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(command.Id, ct);
        if (entity is null) return null;

        if (entity.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to update this prospect");

        entity.UpdateBasics(
            name: command.Name,
            websiteUrls: command.Websites,
            notes: command.Notes,
            mailTitle: command.MailTitle,
            mailBodyPlain: command.MailBodyPlain,
            mailBodyHTML: command.MailBodyHtml,
            linkedInMessage: command.LinkedInMessage
        );

        if (command.Status.HasValue)
            entity.SetStatus(command.Status.Value);

        await _repo.UpdateAsync(entity, ct);
        return ProspectViewDto.FromEntity(entity);
    }
}
