namespace Esatto.Outreach.Application.Features.Intelligence.UpdateCompanyInfo;

public sealed record UpdateCompanyInfoCommand(
    string Name,
    string Overview,
    string ValueProposition
);
