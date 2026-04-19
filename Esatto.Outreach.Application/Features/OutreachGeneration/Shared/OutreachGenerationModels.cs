using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Application.Features.Intelligence.Shared;

namespace Esatto.Outreach.Application.Features.OutreachGeneration.Shared;

public record ProspectInfo(
    Guid ProspectId,
    string Name,
    string? About,
    string? PictureURL,
    List<string>? Websites,
    List<string>? Tags,
    string? Notes
);

public record ContactPersonContext(
    string Name,
    string? Title,
    string? Email,
    List<string>? PersonalHooks,
    List<string>? PersonalNews,
    string? Summary
);

public record CustomOutreachDraftDto(
   string Title,
   string BodyPlain,
   string BodyHTML,
   OutreachChannel Channel = default
);

// A single previously-generated step in a sequence thread.
public sealed record PriorTurn(
    int StepNumber,
    SequenceStepType StepType,
    int DelayInDays,
    string? Subject,
    string Body
);

// For one-off cold outreach generation (email or LinkedIn for a single prospect).
public sealed record ColdOutreachContext
{
    public required ProspectInfo Prospect { get; init; }
    public required CompanyInfoDto CompanyInfo { get; init; }
    public required string Instructions { get; init; }
    public required OutreachChannel Channel { get; init; }
    public List<ProjectCaseDto>? ProjectCases { get; init; }
    public EntityIntelligence? EntityIntelligence { get; init; }
    public ContactPersonContext? ActiveContact { get; init; }
    public string? UserFullName { get; init; }
}

// For generating a step in a focused sequence (single prospect, builds a conversation thread).
public sealed record FocusedSequenceStepContext
{
    public required ProspectInfo Prospect { get; init; }
    public required CompanyInfoDto CompanyInfo { get; init; }
    public required string Instructions { get; init; }
    public required OutreachChannel Channel { get; init; }
    public required int StepNumber { get; init; }
    public required int TotalSteps { get; init; }
    public required SequenceStepType StepType { get; init; }
    public required int DelayInDays { get; init; }
    public List<ProjectCaseDto>? ProjectCases { get; init; }
    public EntityIntelligence? EntityIntelligence { get; init; }
    public ContactPersonContext? ActiveContact { get; init; }
    public string? UserFullName { get; init; }
    public List<PriorTurn> PriorTurns { get; init; } = [];
}
