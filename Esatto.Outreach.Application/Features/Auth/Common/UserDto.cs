namespace Esatto.Outreach.Application.Features.Auth;

public record UserDto(
    string Id,
    string Email,
    string? FullName
);
