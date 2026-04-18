namespace Esatto.Outreach.Application.Features.ProjectCases.CreateProjectCase;

public sealed record CreateProjectCaseCommand(
    string ClientName,
    string Text,
    bool IsActive
);
