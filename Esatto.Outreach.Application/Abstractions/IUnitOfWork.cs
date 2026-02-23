namespace Esatto.Outreach.Application.Abstractions;

public interface IUnitOfWork
{
    Task<System.Data.Common.DbTransaction?> BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
