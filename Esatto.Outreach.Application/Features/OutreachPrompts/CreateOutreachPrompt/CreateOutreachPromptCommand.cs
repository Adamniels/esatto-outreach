using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.OutreachPrompts.CreateOutreachPrompt;

public sealed record CreateOutreachPromptCommand(
    string Instructions,
    PromptType Type,
    bool IsActive = false
);
