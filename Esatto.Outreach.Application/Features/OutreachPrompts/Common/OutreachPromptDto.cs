using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.OutreachPrompts;

public record OutreachPromptDto(
    Guid Id,
    string Instructions,
    PromptType Type,
    bool IsActive,
    DateTime CreatedUtc,
    DateTime UpdatedUtc
);
