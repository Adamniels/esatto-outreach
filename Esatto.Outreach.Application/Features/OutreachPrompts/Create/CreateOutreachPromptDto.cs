using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.OutreachPrompts;

public record CreateOutreachPromptDto(
    string Instructions,
    PromptType Type,
    bool IsActive = false
);
