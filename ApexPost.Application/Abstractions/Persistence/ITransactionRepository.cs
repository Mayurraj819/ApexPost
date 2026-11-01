using ApexPost.Domain.Entities;

namespace ApexPost.Application.Abstractions.Persistence;
public interface ITransactionRepository : IRepository<Transaction>
{
    Task<bool> HasProductHistoryAsync(Guid productId, CancellationToken ct = default);
}
