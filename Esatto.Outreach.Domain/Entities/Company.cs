using Esatto.Outreach.Domain.Common;
namespace Esatto.Outreach.Domain.Entities;

public class Company : Entity
{
    public string Name { get; set; } = default!;

    public CompanyInformation? CompanyInformation { get; set; }
    public List<ProjectCase> ProjectCases { get; set; } = new();
}