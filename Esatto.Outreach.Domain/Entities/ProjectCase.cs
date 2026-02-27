using Esatto.Outreach.Domain.Common;
namespace Esatto.Outreach.Domain.Entities;

public class ProjectCase : Entity
{
    public string ClientName { get; set; } = default!;
    public string Text { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;
}