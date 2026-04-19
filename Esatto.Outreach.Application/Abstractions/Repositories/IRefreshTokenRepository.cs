using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Abstractions.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default);
    Task UpdateAsync(RefreshToken refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Atomically revokes a refresh token only if it is currently active (not revoked and not expired).
    /// Returns false if no row matched, e.g. another request already rotated the token.
    /// </summary>
    Task<bool> TryRevokeActiveTokenAsync(Guid refreshTokenId, DateTime utcNow, CancellationToken ct = default);
}
