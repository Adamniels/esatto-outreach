using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases;

public class UpdateProspect
{
    private readonly IProspectRepository _repo;
    public UpdateProspect(IProspectRepository repo) => _repo = repo;

    public async Task<ProspectViewDto?> Handle(Guid id, ProspectUpdateDto dto, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity is null) return null;

        // Only pass non-null values to UpdateBasics
        entity.UpdateBasics(
            companyName: dto.CompanyName,
            domain: dto.Domain,
            contactName: dto.ContactName,
            contactEmail: dto.ContactEmail,
            linkedinUrl: dto.LinkedinUrl,
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
