using Esatto.Outreach.Domain.Common;
using System.Security.Cryptography;
using System.Text;

namespace Esatto.Outreach.Domain.Entities;

/// <summary>
/// Refresh token for maintaining user sessions.
/// </summary>
public sealed class RefreshToken : Entity
{
    /// <summary>
    /// SHA-256 hash of the refresh token.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;
    
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

    public static string ComputeTokenHash(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required", nameof(token));

        var bytes = Encoding.UTF8.GetBytes(token.Trim());
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
