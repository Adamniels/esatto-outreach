using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.UseCases;
public class GenerateMailOpenAIResponeAPI
{
    private readonly IProspectRepository _repo;
    private readonly ICustomEmailGenerator _client;

    public GenerateMailOpenAIResponeAPI(IProspectRepository repo, ICustomEmailGenerator client)
    {
        _repo = repo;
        _client = client;
    }

    // Alternativ A: returnera utkastet, spara även på entiteten
    public async Task<ProspectViewDto> Handle(Guid id, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Prospect with id {id} not found");

        var request = BuildRequestFromEntity(entity);

        var draft = await _client.GenerateAsync(request, ct);

        // Spara utkastet på entiteten (valfritt men rekommenderat)
        entity.UpdateBasics(
            mailTitle: draft.Title,
            mailBodyPlain: draft.BodyPlain,
            mailBodyHTML: draft.BodyHTML
        );

        await _repo.UpdateAsync(entity, ct);

        return ProspectViewDto.FromEntity(entity);
    }

    private static CustomEmailRequestDto BuildRequestFromEntity(Prospect e)
    {
        return new CustomEmailRequestDto(
            CompanyName: e.CompanyName,
            Domain: e.Domain,
            ContactName: e.ContactName,
            ContactEmail: e.ContactEmail,
            LinkedinUrl: e.LinkedinUrl,
            Notes: e.Notes
        );
    }

}
