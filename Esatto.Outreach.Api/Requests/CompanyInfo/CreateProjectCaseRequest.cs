namespace Esatto.Outreach.Api.Requests.CompanyInfo;

public sealed record CreateProjectCaseRequest(
    string ClientName,
    string Text,
    bool IsActive
);
