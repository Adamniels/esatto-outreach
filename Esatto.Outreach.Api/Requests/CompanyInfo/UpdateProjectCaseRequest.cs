namespace Esatto.Outreach.Api.Requests.CompanyInfo;

public sealed record UpdateProjectCaseRequest(
    string ClientName,
    string Text,
    bool IsActive
);
