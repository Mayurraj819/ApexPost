using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Contracts;

namespace ApexPost.Infrastructure.Persistence;

public sealed class JsonMoneyOrderRepository : IMoneyOrderRepository
{
    private readonly IJsonStore<MoneyOrderResult> _store;

    public JsonMoneyOrderRepository(IJsonStore<MoneyOrderResult> store)
    {
        _store = store;
    }

    public async Task AddAsync(MoneyOrderResult moneyOrder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(moneyOrder);
        var moneyOrders = (await _store.ReadAsync(cancellationToken)).ToList();
        if (moneyOrders.Any(item => item.Reference == moneyOrder.Reference))
        {
            throw new InvalidOperationException($"A money order with reference '{moneyOrder.Reference}' already exists.");
        }

        moneyOrders.Add(moneyOrder);
        await _store.WriteAsync(moneyOrders, cancellationToken);
    }

    public async Task<MoneyOrderResult?> GetByReferenceAsync(Guid reference, CancellationToken cancellationToken = default) =>
        (await _store.ReadAsync(cancellationToken)).SingleOrDefault(item => item.Reference == reference);

    public async Task<IReadOnlyList<MoneyOrderResult>> ListByBranchAsync(Guid branchId, CancellationToken cancellationToken = default) =>
        (await _store.ReadAsync(cancellationToken)).Where(item => item.BranchId == branchId).ToArray();
}
