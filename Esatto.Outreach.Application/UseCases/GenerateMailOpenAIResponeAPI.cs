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

    public async Task<ProspectViewDto> Handle(Guid id, CustomEmailRequestDto dto, CancellationToken ct = default)
    {
        // TODO: Gör inte vad den ska göra här, inte klar
        if (string.IsNullOrWhiteSpace(dto.CompanyName))
            throw new ArgumentException("CompanyName is required");

        // TODO: Generate the email

        // get entity       
        var entity = await _repo.GetByIdAsync(id, ct);

        // TODO: add the genereted Mail to the entity

        // TODO: save the entity with the updated fields
        var saved = await _repo.AddAsync(entity, ct);
        return ProspectViewDto.FromEntity(saved);
    }
}
