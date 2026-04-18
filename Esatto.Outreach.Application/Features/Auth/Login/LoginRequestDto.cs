namespace Esatto.Outreach.Application.Features.Auth;

public record LoginRequestDto(
    string Email,
    string Password
);
