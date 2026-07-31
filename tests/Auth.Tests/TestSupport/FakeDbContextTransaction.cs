using Microsoft.EntityFrameworkCore.Storage;

namespace Auth.Tests.TestSupport;

public sealed class FakeDbContextTransaction : IDbContextTransaction
{
    public Guid TransactionId { get; } = Guid.NewGuid();

    public int CommitCount { get; private set; }
    public int RollbackCount { get; private set; }
    public int DisposeCount { get; private set; }

    public bool Committed => CommitCount > 0;
    public bool RolledBack => RollbackCount > 0;

    public void Commit() => CommitCount++;

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Commit();
        return Task.CompletedTask;
    }

    public void Rollback() => RollbackCount++;

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        Rollback();
        return Task.CompletedTask;
    }

    public void Dispose() => DisposeCount++;

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
