namespace Esatto.Outreach.Application.DTOs.Intelligence;

public record CompanyInfoDto(
    Guid Id,
    string Name,
    string Overview,
    string ValueProposition
);

public record CompanyInfoUpdateDto(
    string Name,
    string Overview,
    string ValueProposition
);

public record ProjectCaseDto(
    Guid Id,
    string ClientName,
    string Text,
    bool IsActive
);

public record ProjectCaseUpdateDto(
    string ClientName,
    string Text,
    bool IsActive
);

