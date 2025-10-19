namespace Esatto.Outreach.Application.DTOs;

public record ChatRequestDto(
    string UserInput,
    bool? UseWebSearch,
    double Temperature,
    int MaxOutputTokens
);

public record ChatResponseDto(
    string Text
);
