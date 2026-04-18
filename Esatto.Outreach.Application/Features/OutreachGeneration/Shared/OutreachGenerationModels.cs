using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Application.Features.Intelligence.Shared;

namespace Esatto.Outreach.Application.Features.OutreachGeneration.Shared;

public record CustomEmailRequestDto(
    Guid ProspectId,
    string Name,
    string? About,
    string? PictureURL,
    List<string>? Websites,
    List<string>? Tags,
    string? Notes
);

public record CustomOutreachDraftDto(
   string Title,
   string BodyPlain,
   string BodyHTML,
   OutreachChannel Channel = default
);

public sealed record OutreachGenerationContext
{
    public required CompanyInfoDto CompanyInfo { get; init; }
    public List<ProjectCaseDto>? ProjectCases { get; init; }
    public required string Instructions { get; init; }
    public required CustomEmailRequestDto Request { get; init; }
    public required OutreachChannel Channel { get; init; }
    public EntityIntelligence? EntityIntelligence { get; init; }
    public ContactPersonContext? ActiveContact { get; init; }
    public string? UserFullName { get; init; }

    public static OutreachGenerationContext Create(
        CompanyInfoDto companyInfo,
        List<ProjectCaseDto>? projectCases,
        string instructions,
        CustomEmailRequestDto request,
        OutreachChannel channel,
        EntityIntelligence? entityIntelligence = null,
        ContactPersonContext? activeContact = null,
        string? userFullName = null)
    {
        return new OutreachGenerationContext
        {
            CompanyInfo = companyInfo,
            ProjectCases = projectCases,
            Instructions = instructions,
            Request = request,
            Channel = channel,
            EntityIntelligence = entityIntelligence,
            ActiveContact = activeContact,
            UserFullName = userFullName
        };
    }
}

public record ContactPersonContext(
    string Name,
    string? Title,
    string? Email,
    List<string>? PersonalHooks,
    List<string>? PersonalNews,
    string? Summary
);
