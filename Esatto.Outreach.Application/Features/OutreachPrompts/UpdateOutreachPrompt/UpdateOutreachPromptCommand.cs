namespace Esatto.Outreach.Application.Features.OutreachPrompts.UpdateOutreachPrompt;

public sealed record UpdateOutreachPromptCommand(
    Guid Id,
    string Instructions
);
