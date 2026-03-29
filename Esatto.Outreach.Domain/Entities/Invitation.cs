using Esatto.Outreach.Domain.Common;

namespace Esatto.Outreach.Domain.Entities;

/// <summary>
/// Invitation for a user to join a company. Token is used in the invite link.
/// </summary>
public sealed class Invitation : Entity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string Email { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser CreatedBy { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
