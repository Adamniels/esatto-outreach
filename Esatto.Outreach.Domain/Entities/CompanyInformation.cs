using Esatto.Outreach.Domain.Common;
namespace Esatto.Outreach.Domain.Entities;

public class CompanyInformation : Entity
{

    public string Overview { get; set; } = default!; 
    public string ValueProposition { get; set; } = default!;

    // NOTE: need to make sure a company can only have one company information
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;
}