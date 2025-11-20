using Esatto.Outreach.Domain.Common;

namespace Esatto.Outreach.Domain.Entities;

/// <summary>
/// Refresh token for maintaining user sessions.
/// </summary>
public sealed class RefreshToken : Entity
{
    /// <summary>
    /// The actual token string.
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID this token belongs to.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// When this token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Whether this token has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }
    
    /// <summary>
    /// Navigation property to user.
    /// </summary>
    public ApplicationUser User { get; set; } = null!;
}
