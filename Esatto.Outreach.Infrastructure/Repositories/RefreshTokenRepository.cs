using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly OutreachDbContext _db;

    public RefreshTokenRepository(OutreachDbContext db) => _db = db;

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        var tokenHash = RefreshToken.ComputeTokenHash(token);
        return await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        await _db.RefreshTokens.AddAsync(refreshToken, ct);
    }

    public Task UpdateAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _db.RefreshTokens.Update(refreshToken);
        return Task.CompletedTask;
    }

    public async Task<bool> TryRevokeActiveTokenAsync(Guid refreshTokenId, DateTime utcNow, CancellationToken ct = default)
    {
        return await _db.RefreshTokens
            .Where(rt => rt.Id == refreshTokenId && !rt.IsRevoked && rt.ExpiresAt > utcNow)
            .ExecuteUpdateAsync(
                s => s.SetProperty(rt => rt.IsRevoked, true)
                    .SetProperty(rt => rt.UpdatedUtc, utcNow),
                ct) > 0;
    }
}
