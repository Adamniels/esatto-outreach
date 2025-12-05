using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Prospects;

public class UpdateProspect
{
    private readonly IProspectRepository _repo;
    public UpdateProspect(IProspectRepository repo) => _repo = repo;

    public async Task<ProspectViewDto?> Handle(Guid id, ProspectUpdateDto dto, string userId, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity is null) return null;

        // ========== OWNERSHIP CHECK ==========
        if (entity.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to update this prospect");
        // =====================================

        // Only pass non-null values to UpdateBasics
        entity.UpdateBasics(
            name: dto.Name,
            websiteUrls: dto.Websites,
            emailAddresses: dto.EmailAddresses,
            phoneNumbers: dto.PhoneNumbers,
            notes: dto.Notes,
            mailTitle: dto.MailTitle,
            mailBodyPlain: dto.MailBodyPlain,
            mailBodyHTML: dto.MailBodyHTML
        );

        if (dto.Status.HasValue)
            entity.SetStatus(dto.Status.Value);

        await _repo.UpdateAsync(entity, ct);
        return ProspectViewDto.FromEntity(entity);
    }
}
