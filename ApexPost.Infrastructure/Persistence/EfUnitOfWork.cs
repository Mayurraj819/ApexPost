using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Infrastructure.Persistence;
public sealed class EfUnitOfWork(ApexPostDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("This stock or transaction changed. Refresh and retry.", ex);
        }
        catch (DbUpdateException ex)when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new UniqueConflictException("A record with this unique value already exists.", ex);
        }
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        // This callback is replayed as a whole. Current callers issue SQL/ExecuteUpdate only.
        // Reject tracked writes: replaying domain mutations and accepted EF state is unsafe.
        if (db.ChangeTracker.HasChanges() || db.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("Finish tracked writes before starting a replayable SQL operation.");
        var operationId = Guid.NewGuid();
        var strategy = db.Database.CreateExecutionStrategy();
        db.IsReplayableSqlOperation = true;
        try
        {
            await strategy.ExecuteInTransactionAsync(
                async token =>
                {
                    await db.Database.ExecuteSqlAsync(
                        $"INSERT dbo.OperationReceipts (Id) VALUES ({operationId})", token);
                    await operation(token);
                },
                token => db.Set<ApexPost.Infrastructure.Persistence.Entities.OperationReceipt>()
                    .AsNoTracking().AnyAsync(x => x.Id == operationId, token),
                cancellationToken);
        }
        finally
        {
            db.IsReplayableSqlOperation = false;
        }
    }
}
