using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.OutreachGeneration.Shared;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Text;

namespace Esatto.Outreach.Infrastructure.Services.OutreachGeneration;

public sealed class OpenAIColdOutreachGenerator : OpenAIOutreachGeneratorBase, IColdOutreachGenerator
{
    public OpenAIColdOutreachGenerator(
        HttpClient http,
        IOptions<OpenAiOptions> options)
        : base(http, options)
    {
    }

    public async Task<CustomOutreachDraftDto> GenerateAsync(ColdOutreachContext context, CancellationToken ct = default)
    {
        bool useWebSearch = context.Strategy == OutreachGenerationType.WebSearch;

        if (context.Strategy == OutreachGenerationType.UseCollectedData && context.EntityIntelligence == null)
            throw new InvalidOperationException("Strategy is UseCollectedData but no Entity Intelligence is in context. Generate it first.");

        var prompt = BuildPrompt(context) + $"\n\n{BuildJsonInstruction(context.Channel)}";

        var jsonText = await CallWithStringInputAsync(prompt, useWebSearch, ct);

        return ParseAndValidate(jsonText, context.Channel, $"Introduktion till {context.Prospect.Name}");
    }

    private static string BuildPrompt(ColdOutreachContext context)
    {
        var req = context.Prospect;
        var sb = new StringBuilder();

        string targetFormat = context.Channel == OutreachChannel.Email ? "sälj mejl" : "LinkedIn-meddelande";
        sb.AppendLine($"Du är en säljare på {context.CompanyInfo.Name} och ska skriva ett kort, personligt {targetFormat} på svenska (max 500 ord).");
        sb.AppendLine();
        sb.AppendLine($"=== OM OSS ({context.CompanyInfo.Name}) ===");
        sb.AppendLine(context.CompanyInfo.Overview);
        sb.AppendLine(context.CompanyInfo.ValueProposition);
        sb.AppendLine();
        sb.AppendLine("=== CASE STUDIES ===");
        sb.AppendLine(FormatProjectCases(context.ProjectCases));
        sb.AppendLine();
        sb.AppendLine("=== MÅLFÖRETAG ===");
        sb.AppendLine($"Företag: {req.Name}");
        if (!string.IsNullOrWhiteSpace(req.About)) sb.AppendLine($"Om företaget: {req.About}");
        if (req.Websites?.Any() == true) sb.AppendLine($"Webbplatser: {string.Join(", ", req.Websites)}");
        if (req.Tags?.Any() == true) sb.AppendLine($"Taggar: {string.Join(", ", req.Tags)}");
        if (!string.IsNullOrWhiteSpace(req.Notes)) sb.AppendLine($"Anteckningar: {req.Notes}");

        if (context.Strategy == OutreachGenerationType.UseCollectedData && context.EntityIntelligence != null)
        {
            sb.AppendLine();
            sb.AppendLine(BuildCollectedDataSection(context.EntityIntelligence));
        }

        if (context.ActiveContact != null)
        {
            sb.AppendLine();
            sb.AppendLine(BuildContactSection(context.ActiveContact));
        }

        sb.AppendLine();
        sb.AppendLine("=== INSTRUKTIONER ===");
        sb.AppendLine(context.Instructions);
        sb.AppendLine();
        sb.AppendLine("VIKTIGT:");

        if (context.Strategy == OutreachGenerationType.UseCollectedData)
        {
            sb.AppendLine("- Använd informationen under INSAMLAD DATA för att hitta en konkret koppling till kunden.");
            sb.AppendLine($"- Matcha kundens utmaningar med {context.CompanyInfo.Name}s tjänster.");
        }
        else
        {
            sb.AppendLine($"- Fokusera på hur {context.CompanyInfo.Name} kan hjälpa målföretaget.");
            sb.AppendLine("- Matcha rätt tjänster till kundens situation.");
        }

        sb.AppendLine("- Skriv personligt och engagerande.");

        if (context.ActiveContact != null)
            sb.AppendLine($"- Tilltala kontaktpersonen med namn: {context.ActiveContact.Name}");
        else
            sb.AppendLine("- Ingen kontaktperson angiven — skriv generellt. Använd INTE platshållare som [Namn]. Starta med 'Hej,'.");

        if (!string.IsNullOrWhiteSpace(context.UserFullName))
            sb.AppendLine($"- Signera med: '{context.UserFullName}, {context.CompanyInfo.Name}'");

        var channelInstruction = context.Channel switch
        {
            OutreachChannel.Email => "- Skriv med tydlig ämnesrad och engagerande brödtext.",
            OutreachChannel.LinkedIn => "- Skriv med en hook i början och personlig ton.",
            _ => ""
        };
        if (!string.IsNullOrWhiteSpace(channelInstruction)) sb.AppendLine(channelInstruction);

        return sb.ToString().TrimEnd();
    }

    private static string BuildCollectedDataSection(EntityIntelligence intelligence)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== INSAMLAD DATA OM MÅLFÖRETAGET ===");

        if (intelligence.EnrichedData != null)
        {
            var ed = intelligence.EnrichedData;
            sb.AppendLine($"Snapshot: {ed.Snapshot.WhatTheyDo}. Verksamhet: {ed.Snapshot.HowTheyOperate}. Målkund: {ed.Snapshot.TargetCustomer}.");
            sb.AppendLine($"Affärsmodell: {ed.Profile.BusinessModel} | Kundtyp: {ed.Profile.CustomerType} | Teknik: {ed.Profile.TechnologyPosture}");

            if (ed.Challenges.Confirmed.Any())
                sb.AppendLine($"Bekräftade utmaningar: {string.Join("; ", ed.Challenges.Confirmed.Select(c => c.ChallengeDescription))}");
            if (ed.Challenges.Inferred.Any())
                sb.AppendLine($"Troliga utmaningar: {string.Join("; ", ed.Challenges.Inferred.Select(c => c.ChallengeDescription))}");
            if (ed.OutreachHooks.Any())
                sb.AppendLine($"Relevanta händelser: {string.Join("; ", ed.OutreachHooks.Select(h => $"{h.Date}: {h.HookDescription}"))}");
            sb.AppendLine($"(Data insamlad: {intelligence.ResearchedAt:yyyy-MM-dd})");
        }
        else if (!string.IsNullOrWhiteSpace(intelligence.SummarizedContext))
        {
            sb.AppendLine(intelligence.SummarizedContext);
            sb.AppendLine($"(Data insamlad: {intelligence.ResearchedAt:yyyy-MM-dd})");
        }

        return sb.ToString().TrimEnd();
    }
}
