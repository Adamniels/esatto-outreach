namespace Esatto.Outreach.Application.Features.Intelligence.ChatWithProspect;

public record ChatWithProspectResponse(
    string AiMessage,
    bool ImprovedMail,
    string? MailTitle,
    string? MailBodyPlain,
    string? MailBodyHTML
);
