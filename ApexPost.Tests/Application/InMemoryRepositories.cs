using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Contracts;
using ApexPost.Domain.Entities;

namespace ApexPost.Tests.Application;

internal abstract class InMemoryRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly Dictionary<Guid, TEntity> _items = [];
    private readonly Func<TEntity, Guid> _idSelector;

    protected InMemoryRepository(Func<TEntity, Guid> idSelector) => _idSelector = idSelector;

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<TEntity>>(_items.Values.ToArray());
    }

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var id = _idSelector(entity);
        if (!_items.TryAdd(id, entity))
        {
            throw new InvalidOperationException($"Entity '{id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items[_idSelector(entity)] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items.Remove(id);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryBranchRepository : InMemoryRepository<Branch>, IBranchRepository
{
    public InMemoryBranchRepository() : base(item => item.Id) { }
}

internal sealed class InMemoryCounterRepository : InMemoryRepository<Counter>, ICounterRepository
{
    public InMemoryCounterRepository() : base(item => item.Id) { }
}

internal sealed class InMemoryStaffRepository : InMemoryRepository<Staff>, IStaffRepository
{
    public InMemoryStaffRepository() : base(item => item.Id) { }
}

internal sealed class InMemoryProductRepository : InMemoryRepository<Product>, IProductRepository
{
    public InMemoryProductRepository() : base(item => item.Id) { }
}

internal sealed class InMemoryTransactionRepository : InMemoryRepository<Transaction>, ITransactionRepository
{
    public InMemoryTransactionRepository() : base(item => item.Id) { }
}

internal sealed class InMemoryBranchStockRepository : InMemoryRepository<BranchStock>, IBranchStockRepository
{
    public InMemoryBranchStockRepository() : base(item => item.Id) { }

    public async Task<BranchStock?> GetByBranchAndProductAsync(Guid branchId, Guid productId, CancellationToken cancellationToken = default) =>
        (await ListAsync(cancellationToken)).SingleOrDefault(item => item.BranchId == branchId && item.ProductId == productId);
}

internal sealed class InMemoryMoneyOrderRepository : IMoneyOrderRepository
{
    private readonly Dictionary<Guid, MoneyOrderResult> _items = [];

    public Task AddAsync(MoneyOrderResult moneyOrder, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items.Add(moneyOrder.Reference, moneyOrder);
        return Task.CompletedTask;
    }

    public Task<MoneyOrderResult?> GetByReferenceAsync(Guid reference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items.TryGetValue(reference, out var result);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<MoneyOrderResult>> ListByBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<MoneyOrderResult>>(_items.Values.Where(item => item.BranchId == branchId).ToArray());
    }
}
