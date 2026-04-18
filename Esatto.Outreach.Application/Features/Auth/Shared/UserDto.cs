namespace Esatto.Outreach.Application.Features.Auth.Shared;

public record UserDto(
    string Id,
    string Email,
    string? FullName
);
