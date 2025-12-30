namespace Esatto.Outreach.Application.DTOs.Auth;

public record RegisterRequestDto(
    string Email,
    string Password,
    string? FullName
);

public record LoginRequestDto(
    string Email,
    string Password
);

public record RefreshTokenRequestDto(
    string RefreshToken
);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

public record UserDto(
    string Id,
    string Email,
    string? FullName
);
