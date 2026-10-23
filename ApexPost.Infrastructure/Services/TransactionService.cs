using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Domain.Entities;

namespace ApexPost.Infrastructure.Services;

public sealed class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;

    public TransactionService(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public Task<Transaction?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Transaction>> ListAsync(CancellationToken cancellationToken = default) =>
        _repository.ListAsync(cancellationToken);

    public Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default) =>
        _repository.AddAsync(transaction, cancellationToken);
}
