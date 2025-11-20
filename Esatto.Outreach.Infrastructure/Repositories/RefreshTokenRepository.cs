using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly OutreachDbContext _db;

    public RefreshTokenRepository(OutreachDbContext db) => _db = db;

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, ct);

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        await _db.RefreshTokens.AddAsync(refreshToken, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        _db.RefreshTokens.Update(refreshToken);
        await _db.SaveChangesAsync(ct);
    }
}
