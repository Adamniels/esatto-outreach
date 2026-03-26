using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;

using Esatto.Outreach.Application.DTOs.Intelligence;

namespace Esatto.Outreach.Application.DTOs.Outreach;

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


/// <summary>
/// Context containing all data needed for outreach generation.
/// Use case prepares this context, generator uses it.
/// Follows Clean Architecture: Use case orchestrates data, generator focuses on technical implementation.
/// </summary>
public sealed record OutreachGenerationContext
{
    /// <summary>
    /// Information about Esatto AB (loaded from JSON file)
    /// </summary>
    public required CompanyInfoDto CompanyInfo { get; init; }

    /// <summary>
    /// Relevant project cases to include in email generation (loaded from database)
    /// </summary>
    public List<ProjectCaseDto>? ProjectCases { get; init; }

    /// <summary>
    /// User-specific email generation instructions from database
    /// </summary>
    public required string Instructions { get; init; }

    /// <summary>
    /// Basic prospect information for outreach
    /// </summary>
    public required CustomEmailRequestDto Request { get; init; }

    /// <summary>
    /// The outreach channel (Email, LinkedIn, etc.)
    /// </summary>
    public required OutreachChannel Channel { get; init; }

    /// <summary>
    /// Collected soft data (only present when using UseCollectedData generation type)
    /// </summary>
    public EntityIntelligence? EntityIntelligence { get; init; }

    /// <summary>
    /// Active contact person and their enrichment data (if available)
    /// </summary>
    public ContactPersonContext? ActiveContact { get; init; }

    /// <summary>
    /// Full name of the user generating the email (for signature)
    /// </summary>
    public string? UserFullName { get; init; }

    /// <summary>
    /// Factory method to create context with all required data
    /// </summary>
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

/// <summary>
/// Contains contact person data to include in email generation context.
/// </summary>
public record ContactPersonContext(
    string Name,
    string? Title,
    string? Email,
    List<string>? PersonalHooks,
    List<string>? PersonalNews,
    string? Summary
);
