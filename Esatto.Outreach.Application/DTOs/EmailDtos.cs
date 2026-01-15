using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.DTOs;

public record CustomEmailRequestDto(
    Guid ProspectId,
    string Name,
    string? About,
    string? PictureURL,
    List<string>? Websites,
    List<string>? EmailAddresses,
    List<string>? PhoneNumbers,
    List<string>? Addresses,
    List<string>? Tags,
    string? Notes
);


public record CustomEmailDraftDto(
   string Title,
   string BodyPlain,
   string BodyHTML
);


public record SendOutreachToN8nDTO(
   string To,
   string Subject,
   string Body
);

public record ResponseSendOutreachToN8nDTO(
        bool Success,
        string? Message
);

/// <summary>
/// Context containing all data needed for email generation.
/// Use case prepares this context, generator uses it.
/// Follows Clean Architecture: Use case orchestrates data, generator focuses on technical implementation.
/// </summary>
public sealed record EmailGenerationContext
{
    /// <summary>
    /// Information about Esatto AB (loaded from JSON file)
    /// </summary>
    public required string CompanyInfo { get; init; }

    /// <summary>
    /// User-specific email generation instructions from database
    /// </summary>
    public required string Instructions { get; init; }

    /// <summary>
    /// Basic prospect information for email
    /// </summary>
    public required CustomEmailRequestDto Request { get; init; }

    /// <summary>
    /// Collected soft data (only present when using UseCollectedData generation type)
    /// </summary>
    public EntityIntelligence? EntityIntelligence { get; init; }

    /// <summary>
    /// Factory method to create context with all required data
    /// </summary>
    public static EmailGenerationContext Create(
        string companyInfo,
        string instructions,
        CustomEmailRequestDto request,
        EntityIntelligence? entityIntelligence = null)
    {
        return new EmailGenerationContext
        {
            CompanyInfo = companyInfo,
            Instructions = instructions,
            Request = request,
            EntityIntelligence = entityIntelligence
        };
    }
}
