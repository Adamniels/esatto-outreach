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

    // TODO: ska returnera Task<ProspectViewDto> sen men vill bara se att det funkar nu 
    // Den ska inte heller behöva ta in CustomEmailRequestDto för det kan jag skapa utifrån id här inne
    public async Task<CustomEmailDraftDto> Handle(Guid id, CustomEmailRequestDto dto, CancellationToken ct = default)
    {
        // TODO: Gör inte vad den ska göra här, inte klar
        if (string.IsNullOrWhiteSpace(dto.CompanyName))
            throw new ArgumentException("CompanyName is required");

        // TODO: Generate the email
        // Här anropas din OpenAI-generator som använder Response API + WebSearch.
        // Den returnerar ett färdigt CustomEmailDraftDto med Title, BodyPlain och BodyHTML.
        var draft = await _client.GenerateAsync(dto, ct);

        // get entity       
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
            throw new InvalidOperationException($"Prospect with id {id} not found");

        // TODO: add the genereted Mail to the entity
        // Här skulle man i framtiden kunna lägga till fält på entity, t.ex.:
        // entity.LastGeneratedEmailTitle = draft.Title;
        // entity.LastGeneratedEmailBody = draft.BodyPlain;
        // Men just nu hoppar vi över detta steg för test.

        // TODO: save the entity with the updated fields
        // Här sparas inte något ännu, för vi vill bara testa att OpenAI-delen fungerar.
        // var saved = await _repo.AddAsync(entity, ct);
        // return ProspectViewDto.FromEntity(saved);

        // För tillfället returnerar vi bara utkastet som genererats av OpenAI
        return draft;
    }
}
