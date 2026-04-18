namespace Esatto.Outreach.Application.Features.Auth.Shared;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);
