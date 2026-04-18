namespace Esatto.Outreach.Application.Features.ProjectCases.UpdateProjectCase;

public sealed record UpdateProjectCaseCommand(
    Guid Id,
    string ClientName,
    string Text,
    bool IsActive
);
