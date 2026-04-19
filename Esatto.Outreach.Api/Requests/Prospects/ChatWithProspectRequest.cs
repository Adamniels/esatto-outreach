namespace Esatto.Outreach.Api.Requests.Prospects;

public sealed record ChatWithProspectRequest(
    string UserInput,
    string? MailTitle,
    string? MailBodyPlain,
    bool? UseWebSearch,
    double Temperature,
    int MaxOutputTokens
);
