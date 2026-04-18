using System.Security.Claims;

namespace Esatto.Outreach.Api.Endpoints;

internal static class EndpointUserExtensions
{
    public static string? GetRequiredUserId(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier);
}
