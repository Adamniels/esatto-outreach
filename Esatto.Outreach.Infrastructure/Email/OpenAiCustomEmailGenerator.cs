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
    private static string? _esattoCompanyInfo;
    private static readonly object _lock = new();

    public OpenAICustomEmailGenerator(
        IOpenAIResponseClientFactory factory,
        IOptions<OpenAiOptions> options)
    {
        _factory = factory;
        _options = options.Value;
        LoadEsattoCompanyInfo();
    }

    private static void LoadEsattoCompanyInfo()
    {
        if (_esattoCompanyInfo != null) return;
        
        lock (_lock)
        {
            if (_esattoCompanyInfo != null) return;

            try
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "esatto-company-info.json");
                if (File.Exists(filePath))
                {
                    _esattoCompanyInfo = File.ReadAllText(filePath);
                }
                else
                {
                    _esattoCompanyInfo = "{}"; // Fallback om filen inte hittas
                }
            }
            catch
            {
                _esattoCompanyInfo = "{}";
            }
        }
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

    // TODO: ändra och fixa prompten jobba på tonen
    private static string BuildPrompt(CustomEmailRequestDto req)
    {
        // Huvudprompten (på svenska) för att generera mejlet
        return @$"
            Du är en säljare på Esatto AB och ska skriva ett kort, personligt säljmejl på svenska (max 500 ord).
            
            === INFORMATION OM ESATTO AB ===
            {_esattoCompanyInfo}
            
            === MÅLFÖRETAG ===
            Företag: {req.CompanyName}
            Domän: {req.Domain}
            Kontakt: {req.ContactName} ({req.ContactEmail})
            Anteckningar: {req.Notes}

            === INSTRUKTIONER ===
            Fokusera på hur vi (Esatto AB) kan hjälpa {req.CompanyName}. 
            Använd informationen ovan om Esatto för att:
            - Hitta relevanta cases som liknar kundens bransch eller utmaningar
            - Visa konkret förståelse för kundens behov genom att referera till liknande projekt
            - Matcha rätt tjänster och metoder till kundens situation
            - Skriv i Esattos ton och värderingar (ärlighet, engagemang, omtanke, samarbete)

            Krav:
            - Hook i första meningen.
            - 1–2 konkreta värdeförslag anpassade till företaget.
            - Referera gärna till ett eller två relevant Esatto-case som exempel
            - Avsluta med en enkel call-to-action (t.ex. 'Vill du att jag skickar ett konkret förslag?').";
    }
}
