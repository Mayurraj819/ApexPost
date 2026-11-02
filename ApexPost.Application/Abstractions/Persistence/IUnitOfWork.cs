namespace ApexPost.Application.Abstractions.Persistence;
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    // Callback must contain replayable SQL operations only; no tracked saves or external effects.
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
