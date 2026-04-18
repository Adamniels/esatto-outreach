namespace Esatto.Outreach.Application.Features.OutreachGeneration.GenerateLinkedInDraft;

public sealed record GenerateLinkedInMessageCommand(
    Guid Id,
    string? Type = null
);
