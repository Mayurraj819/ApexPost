using ApexPost.Application.Contracts;

namespace ApexPost.Application.Abstractions.Persistence;

public interface IMoneyOrderRepository
{
    Task AddAsync(MoneyOrderResult moneyOrder, CancellationToken cancellationToken = default);
    Task<MoneyOrderResult?> GetByReferenceAsync(Guid reference, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MoneyOrderResult>> ListByBranchAsync(Guid branchId, CancellationToken cancellationToken = default);
}
