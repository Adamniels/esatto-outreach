using System.Security.Claims;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Abstractions;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generate an access token for the given user.
    /// </summary>
    /// <returns>Tuple containing the token and its expiration time.</returns>
    (string Token, DateTime ExpiresAt) GenerateAccessToken(ApplicationUser user);
    
    /// <summary>
    /// Generate a cryptographically secure refresh token.
    /// </summary>
    string GenerateRefreshToken();
    
    /// <summary>
    /// Get the refresh token expiry date based on configuration.
    /// </summary>
    DateTime GetRefreshTokenExpiryDate();
    
    /// <summary>
    /// Validate a token and return the claims principal.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);
}
