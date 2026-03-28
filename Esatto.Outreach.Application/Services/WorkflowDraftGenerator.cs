using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Services;

/// <summary>
/// Internal service for generating email/LinkedIn drafts for workflow steps.
/// Used by workflow use cases — not exposed directly via endpoints.
/// </summary>
public class WorkflowDraftGenerator
{
    private readonly IOutreachContextBuilder _contextBuilder;
    private readonly IOutreachGeneratorFactory _factory;

    public WorkflowDraftGenerator(IOutreachContextBuilder contextBuilder, IOutreachGeneratorFactory factory)
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

        // 4. Return as tuple
        if (type == WorkflowStepType.Email)
        {
            return (draft.Title, draft.BodyPlain);
        }
        else
        {
            // For LinkedIn, usually no subject.
            return ("", draft.BodyPlain);
        }
    }
}
