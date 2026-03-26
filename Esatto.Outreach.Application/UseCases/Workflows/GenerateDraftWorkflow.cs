using System.Text;
using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Abstractions.Clients;
using Esatto.Outreach.Application.UseCases.Workflows;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class GenerateDraftWorkflow
{
    private readonly IOutreachContextBuilder _contextBuilder;
    private readonly IOutreachGeneratorFactory _factory;

    public GenerateDraftWorkflow(IOutreachContextBuilder contextBuilder, IOutreachGeneratorFactory factory)
    {
        _contextBuilder = contextBuilder;
        _factory = factory;
    }

    public async Task GenerateDraftForStepAsync(WorkflowStep step, Guid prospectId, string? userId, CancellationToken ct)
    {
        var (subject, body) = await GenerateDraftAsync(prospectId, step.Type, userId, step.GenerationStrategy, ct);
        step.UpdateDraft(subject, body);
    }

    public async Task<(string Subject, string Body)> GenerateDraftAsync(
        Guid prospectId,
        WorkflowStepType type,
        string? userId = null,
        ContentGenerationStrategy? strategy = null,
        CancellationToken ct = default)
    {
        // 1. Build Context
        var uid = userId ?? "System";
        var strategyKey = strategy?.ToString() ?? ContentGenerationStrategy.WebSearch.ToString();

        bool includeSoftData = strategy == ContentGenerationStrategy.UseCollectedData;


        var channel = type == WorkflowStepType.Email ? OutreachChannel.Email : OutreachChannel.LinkedIn;

        var context = await _contextBuilder.BuildContextAsync(prospectId, uid, channel, includeSoftData: includeSoftData, cancellationToken: ct);

        // 2. Get Generator based on Strategy (Default to WebSearch if null)
        var generator = _factory.GetGenerator(strategyKey);

        // 3. Generate
        var draft = await generator.GenerateAsync(context, ct);

        // 4. Return as tuple (Mapping CustomOutreachDraftDto to Subject/Body)
        // If it's a LinkedIn message, we might ignore the title/subject or map it differently.
        // But for consistency, we'll return what the generator produced.
        // Typically Title map to Subject, BodyPlain/BodyHTML map to Body.

        if (type == WorkflowStepType.Email)
        {
            return (draft.Title, draft.BodyPlain);
        }
        else
        {
            // For LinkedIn, usually no subject.
            // If the generator returns a Title, we might just ignore it or append it?
            // Usually LinkedIn messages just have body. 
            // Let's assume BodyPlain is the message.
            return ("", draft.BodyPlain);
        }
    }
}
