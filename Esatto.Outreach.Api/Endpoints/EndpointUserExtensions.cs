using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Esatto.Outreach.Api.Endpoints;

internal static class EndpointUserExtensions
{
    /// <summary>
    /// Tries to read the authenticated user id from <see cref="ClaimTypes.NameIdentifier"/> (e.g. JWT <c>sub</c>).
    /// </summary>
    /// <returns><see langword="true"/> when a non-empty id was found.</returns>
    public static bool TryGetUserId(this ClaimsPrincipal user, [NotNullWhen(true)] out string? userId)
    {
        userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrEmpty(userId);
    }
}
