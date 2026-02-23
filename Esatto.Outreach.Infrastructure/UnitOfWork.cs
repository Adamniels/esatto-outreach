using Esatto.Outreach.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Esatto.Outreach.Infrastructure;

public sealed class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly OutreachDbContext _dbContext;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(OutreachDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<System.Data.Common.DbTransaction?> BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction != null)
        {
            return _currentTransaction.GetDbTransaction();
        }

        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(ct);
        return _currentTransaction.GetDbTransaction();
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.CommitAsync(ct);
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync(ct);
            }
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
        }
    }
}
