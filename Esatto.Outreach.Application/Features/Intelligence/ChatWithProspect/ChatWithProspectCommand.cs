namespace Esatto.Outreach.Application.Features.Intelligence.ChatWithProspect;

public sealed record ChatWithProspectCommand(
    Guid ProspectId,
    string UserInput,
    string? MailTitle,
    string? MailBodyPlain,
    bool? UseWebSearch,
    double Temperature,
    int MaxOutputTokens
);
