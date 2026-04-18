namespace Esatto.Outreach.Application.Features.Auth;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);
