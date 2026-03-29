using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.DTOs.Outreach;

public record OutreachPromptDto(
    Guid Id,
    string Instructions,
    PromptType Type,
    bool IsActive,
    DateTime CreatedUtc,
    DateTime UpdatedUtc
);

public record CreateOutreachPromptDto(
    string Instructions,
    PromptType Type,
    bool IsActive = false
);

public record UpdateOutreachPromptDto(
    string Instructions
);
