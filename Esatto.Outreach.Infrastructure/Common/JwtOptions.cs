namespace Esatto.Outreach.Infrastructure.Common;

/// <summary>
/// JWT authentication configuration.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    
    /// <summary>
    /// Secret key for signing tokens (min 32 characters).
    /// </summary>
    public string Secret { get; set; } = string.Empty;
    
    /// <summary>
    /// Token issuer (typically your API URL).
    /// </summary>
    public string Issuer { get; set; } = "EsattoOutreachAPI";
    
    /// <summary>
    /// Token audience (typically your frontend URL).
    /// </summary>
    public string Audience { get; set; } = "EsattoOutreachApp";
    
    /// <summary>
    /// Access token expiry in minutes. Default: 60 (1 hour).
    /// </summary>
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    
    /// <summary>
    /// Refresh token expiry in days. Default: 7.
    /// </summary>
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
