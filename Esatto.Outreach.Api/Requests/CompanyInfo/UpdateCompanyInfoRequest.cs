namespace Esatto.Outreach.Api.Requests.CompanyInfo;

public sealed record UpdateCompanyInfoRequest(
    string Name,
    string Overview,
    string ValueProposition
);
