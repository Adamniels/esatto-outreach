namespace Esatto.Outreach.Application.Features.OutreachGeneration.GenerateEmailDraft;

public sealed record GenerateMailCommand(
    Guid Id,
    string? Type = null
);
