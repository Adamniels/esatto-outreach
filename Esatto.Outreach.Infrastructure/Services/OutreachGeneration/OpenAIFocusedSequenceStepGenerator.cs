using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.OutreachGeneration.Shared;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Text;

namespace Esatto.Outreach.Infrastructure.Services.OutreachGeneration;

public sealed class OpenAIFocusedSequenceStepGenerator : OpenAIOutreachGeneratorBase, IFocusedSequenceStepGenerator
{
    public OpenAIFocusedSequenceStepGenerator(
        HttpClient http,
        IOptions<OpenAiOptions> options)
        : base(http, options)
    {
    }

    public async Task<CustomOutreachDraftDto> GenerateAsync(
        FocusedSequenceStepContext context,
        CancellationToken ct = default)
    {
        var messages = BuildMessageArray(context);
        var jsonText = await CallWithMessageArrayAsync(messages, ct);
        return ParseAndValidate(jsonText, context.Channel, $"Uppföljning — {context.Prospect.Name}");
    }

    private IReadOnlyList<object> BuildMessageArray(FocusedSequenceStepContext context)
    {
        var messages = new List<object>();
        var systemContext = BuildSystemContext(context);

        var step1Type = context.PriorTurns.Count > 0 ? context.PriorTurns[0].StepType : context.StepType;
        var step1Instruction = $"Detta är en {context.TotalSteps}-stegs outreach-sekvens. Skriv steg 1 — ett {FormatStepType(step1Type)} som introduktion. Tänk på att det kommer {context.TotalSteps - 1} uppföljningssteg efter detta.";

        var jsonInstruction = BuildJsonInstruction(context.Channel);

        if (context.PriorTurns.Count == 0)
        {
            messages.Add(new { role = "user", content = $"{systemContext}\n\n{step1Instruction}\n\n{jsonInstruction}" });
            return messages;
        }

        messages.Add(new { role = "user", content = $"{systemContext}\n\n{step1Instruction}" });

        for (int i = 0; i < context.PriorTurns.Count; i++)
        {
            var turn = context.PriorTurns[i];

            var assistantContent = turn.StepType == SequenceStepType.Email && !string.IsNullOrWhiteSpace(turn.Subject)
                ? $"Ämne: {turn.Subject}\n\n{turn.Body}"
                : turn.Body;

            messages.Add(new { role = "assistant", content = assistantContent });

            bool isLastPriorTurn = (i == context.PriorTurns.Count - 1);
            int nextStepNumber = turn.StepNumber + 1;
            SequenceStepType nextStepType = isLastPriorTurn ? context.StepType : context.PriorTurns[i + 1].StepType;
            int delayOfNextStep = isLastPriorTurn ? context.DelayInDays : context.PriorTurns[i + 1].DelayInDays;
            bool isFinalStep = nextStepNumber == context.TotalSteps;

            var followUpInstruction = BuildFollowUpInstruction(nextStepNumber, context.TotalSteps, nextStepType, delayOfNextStep, isFinalStep);

            var userMessage = isLastPriorTurn
                ? $"{followUpInstruction}\n\n{jsonInstruction}"
                : followUpInstruction;

            messages.Add(new { role = "user", content = userMessage });
        }

        return messages;
    }

    private static string BuildFollowUpInstruction(int stepNumber, int totalSteps, SequenceStepType stepType, int delayInDays, bool isFinalStep)
    {
        var delayText = delayInDays <= 0
            ? "Direkt efteråt"
            : $"{delayInDays} dag{(delayInDays == 1 ? "" : "ar")} har gått utan svar";

        var finalNote = isFinalStep
            ? " Det här är det sista steget."
            : string.Empty;

        return $"{delayText}. Skriv steg {stepNumber} av {totalSteps} — ett {FormatStepType(stepType)} som uppföljning. Upprepa inte vad som redan sagts; bygg vidare naturligt.{finalNote}";
    }

    private static string BuildSystemContext(FocusedSequenceStepContext context)
    {
        var req = context.Prospect;
        var sb = new StringBuilder();
        sb.AppendLine($"Du är en säljare på {context.CompanyInfo.Name} och skriver en {context.TotalSteps}-stegs outreach-sekvens på svenska (max 300 ord per meddelande).");
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
            var intel = context.EntityIntelligence;
            sb.AppendLine();
            sb.AppendLine("=== INSAMLAD DATA ===");
            if (intel.EnrichedData != null)
            {
                var ed = intel.EnrichedData;
                sb.AppendLine($"Snapshot: {ed.Snapshot.WhatTheyDo}. Verksamhet: {ed.Snapshot.HowTheyOperate}. Målkund: {ed.Snapshot.TargetCustomer}.");
                sb.AppendLine($"Affärsmodell: {ed.Profile.BusinessModel} | Kundtyp: {ed.Profile.CustomerType} | Teknik: {ed.Profile.TechnologyPosture}");
                if (ed.Challenges.Confirmed.Any())
                    sb.AppendLine($"Bekräftade utmaningar: {string.Join("; ", ed.Challenges.Confirmed.Select(c => c.ChallengeDescription))}");
                if (ed.Challenges.Inferred.Any())
                    sb.AppendLine($"Troliga utmaningar: {string.Join("; ", ed.Challenges.Inferred.Select(c => c.ChallengeDescription))}");
                if (ed.OutreachHooks.Any())
                    sb.AppendLine($"Relevanta händelser: {string.Join("; ", ed.OutreachHooks.Select(h => $"{h.Date}: {h.HookDescription}"))}");
                sb.AppendLine($"(Data insamlad: {intel.ResearchedAt:yyyy-MM-dd})");
            }
            else if (!string.IsNullOrWhiteSpace(intel.SummarizedContext))
            {
                sb.AppendLine(intel.SummarizedContext);
                sb.AppendLine($"(Data insamlad: {intel.ResearchedAt:yyyy-MM-dd})");
            }
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
        sb.AppendLine("- Skriv på svenska.");
        sb.AppendLine("- Max 300 ord per meddelande.");
        sb.AppendLine("- Varje uppföljning ska ha ny vinkel — upprepa inte tidigare meddelanden.");

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

        if (context.ActiveContact != null)
            sb.AppendLine($"- Tilltala kontaktpersonen med namn: {context.ActiveContact.Name}");
        else
            sb.AppendLine("- Ingen kontaktperson angiven — skriv generellt. Använd INTE platshållare som [Namn].");

        if (!string.IsNullOrWhiteSpace(context.UserFullName))
            sb.AppendLine($"- Signera med: '{context.UserFullName}, {context.CompanyInfo.Name}'");

        return sb.ToString().TrimEnd();
    }
}
