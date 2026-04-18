using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.OutreachPrompts.CreateOutreachPrompt;

public record CreateOutreachPromptRequest(
    string Instructions,
    PromptType Type,
    bool IsActive = false
);
