namespace Esatto.Outreach.Application.DTOs;

// TODO: är webSearch på som standard?
public record ChatRequestDto(
    string UserInput,
    string? MailTitle,
    string? MailBodyPlain,
    bool? UseWebSearch,
    double Temperature,
    int MaxOutputTokens
);

public record ChatResponseDto(
    string AiMessage,
    bool ImprovedMail,
    string? MailTitle,
    string? MailBodyPlain,
    string? MailBodyHTML
);
