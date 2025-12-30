using Microsoft.AspNetCore.Identity;

namespace Esatto.Outreach.Domain.Entities;

/// <summary>
/// Application user extending ASP.NET Core Identity.
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
    /// <summary>
    /// User's full name (optional).
    /// </summary>
    public string? FullName { get; set; }
    
    /// <summary>
    /// When the user was created.
    /// </summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last login timestamp.
    /// </summary>
    public DateTime? LastLoginUtc { get; set; }
}
