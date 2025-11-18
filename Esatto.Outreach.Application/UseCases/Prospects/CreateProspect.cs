using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.UseCases.Prospects;

public class CreateProspect
{
    private readonly IProspectRepository _repo;
    public CreateProspect(IProspectRepository repo) => _repo = repo;

    public async Task<ProspectViewDto> Handle(ProspectCreateDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.CompanyName))
            throw new ArgumentException("CompanyName is required");

        var entity = Prospect.Create(
            companyName: dto.CompanyName,
            domain: dto.Domain,
            contactName: dto.ContactName,
            contactEmail: dto.ContactEmail,
            linkedinUrl: dto.LinkedinUrl,
            notes: dto.Notes
        );

        var saved = await _repo.AddAsync(entity, ct);
        return ProspectViewDto.FromEntity(saved);
    }
}
