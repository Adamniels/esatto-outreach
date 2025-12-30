namespace Esatto.Outreach.Application.DTOs;

public record EmailPromptDto(
    Guid Id,
    string Instructions,
    bool IsActive,
    DateTime CreatedUtc,
    DateTime UpdatedUtc
);

public record CreateEmailPromptDto(
    string Instructions,
    bool IsActive = false
);

public record UpdateEmailPromptDto(
    string Instructions
);
