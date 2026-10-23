using ApexPost.Domain.Entities;

namespace ApexPost.Application.Abstractions.Services;

public interface ITransactionService
{
    Task<Transaction?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
