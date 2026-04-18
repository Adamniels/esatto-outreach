namespace Esatto.Outreach.Application.Features.Intelligence.ChatWithProspect;

public record ChatWithProspectRequest(
    string UserInput,
    string? MailTitle,
    string? MailBodyPlain,
    bool? UseWebSearch,
    double Temperature,
    int MaxOutputTokens
);
