namespace Esatto.Outreach.Application.DTOs;

/// <summary>
/// DTO representing the complete company information from JSON file
/// </summary>
public record CompanyInfoDto(
    string Overview,
    List<CaseItemDto> Cases
);

/// <summary>
/// DTO representing a single case/service entry from company info
/// </summary>
public record CaseItemDto(
    string PageTitle,
    string PageType,
    CaseDetailDto Case,
    List<string> Services,
    List<string> Industries,
    List<string> MethodsOrTech,
    List<string> ValuesOrTone
);

/// <summary>
/// DTO representing the nested case details
/// </summary>
public record CaseDetailDto(
    string Name,
    string Industry,
    string Challenge,
    string Solution,
    string Result
);
