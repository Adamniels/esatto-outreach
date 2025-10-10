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

        entity.UpdateBasics(dto.CompanyName, dto.Domain, dto.ContactName, dto.ContactEmail, dto.LinkedinUrl, dto.Notes, dto.MailTitle, dto.MailBodyPlain, dto.MailBodyHTML);

        if (dto.Status.HasValue)
            entity.SetStatus(dto.Status.Value);

        await _repo.UpdateAsync(entity, ct);
        return ProspectViewDto.FromEntity(entity);
    }
}
