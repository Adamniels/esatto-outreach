using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Infrastructure.Email;

public sealed class OpenAICustomEmailGenerator : ICustomEmailGenerator
{
    private readonly IOpenAIResponseClientFactory _factory;
    private readonly OpenAiOptions _options;

    public OpenAICustomEmailGenerator(
        IOpenAIResponseClientFactory factory,
        IOptions<OpenAiOptions> options)
    {
        _factory = factory;
        _options = options.Value;
    }

    public async Task<CustomEmailDraftDto> GenerateAsync(
        CustomEmailRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var client = _factory.GetClient();

        // 1. Bygg upp själva prompten
        var prompt = BuildPrompt(request) + @"

Return ONLY a valid JSON object with the following structure, nothing else:
{
  ""Title"": ""string"",
  ""BodyPlain"": ""string"",
  ""BodyHTML"": ""string""
}
Do not include code fences, explanations, or any extra text.
";

        // 2. Skapa options (inklusive websearch om aktivt)
        var createOptions = new ResponseCreationOptions();
        if (_options.UseWebSearch)
        {
            createOptions.Tools.Add(ResponseTool.CreateWebSearchTool());
        }

        // 3. Kör request mot OpenAI
        OpenAIResponse resp = await client.CreateResponseAsync(
            userInputText: prompt,
            options: createOptions,
            cancellationToken: cancellationToken
        );

        // 4. Plocka ut rå text och trimma
        var jsonText = (resp.GetOutputText() ?? string.Empty).Trim();

        // 5. Försök deserialisera till din DTO
        CustomEmailDraftDto? dto = null;
        try
        {
            dto = JsonSerializer.Deserialize<CustomEmailDraftDto>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse model JSON output: {jsonText}", ex);
        }

        if (dto == null)
            throw new InvalidOperationException($"Model returned null or invalid JSON: {jsonText}");

        // 6. Säkerställ titel om den saknas
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            dto = dto with { Title = $"Introduktion till {request.CompanyName}".Trim() };
        }

        return dto;
    }

    // TODO: ändra och fixa prompten
    private static string BuildPrompt(CustomEmailRequestDto req)
    {
        // Huvudprompten (på svenska) för att generera mejlet
        return @$"
            Skriv ett kort, personligt säljmejl på svenska (max 120 ord).
            Fokusera på hur vi kan hjälpa {req.CompanyName}; konkret och utan fluff.
            Ingen hälsningsfras eller signatur i brödtexten.

            Företag: {req.CompanyName}
            Domän: {req.Domain}
            Kontakt: {req.ContactName} ({req.ContactEmail})
            Anteckningar: {req.Notes}

            Krav:
            - Hook i första meningen.
            - 1–2 konkreta värdeförslag anpassade till företaget.
            - Avsluta med en enkel call-to-action (t.ex. 'Vill du att jag skickar ett konkret förslag?').";
    }
}
