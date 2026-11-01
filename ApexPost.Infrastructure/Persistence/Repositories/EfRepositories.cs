using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Common;
using ApexPost.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Infrastructure.Persistence.Repositories;
public abstract class EfRepository<T>(ApexPostDbContext db) : IRepository<T> where T : Entity
{
    protected ApexPostDbContext Db { get; } = db;
    protected virtual IQueryable<T> Aggregate => Db.Set<T>();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Aggregate.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default) => await Aggregate.ToArrayAsync(cancellationToken);
    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await Db.Set<T>().AddAsync(entity, cancellationToken);
    }

    public virtual Task SaveAsync(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Db.Entry(entity).State == EntityState.Detached)
            throw new InvalidOperationException("Load the tracked aggregate before editing.");
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity is not null)
            Db.Set<T>().Remove(entity);
    }
}

public sealed class EfBranchRepository(ApexPostDbContext db) : EfRepository<Branch>(db), IBranchRepository;
public sealed class EfCounterRepository(ApexPostDbContext db) : EfRepository<Counter>(db), ICounterRepository;
public sealed class EfStaffRepository(ApexPostDbContext db) : EfRepository<Staff>(db), IStaffRepository;
public sealed class EfProductRepository(ApexPostDbContext db) : EfRepository<Product>(db), IProductRepository
{
    public Task<bool> HasStockAdjustmentHistoryAsync(Guid productId, CancellationToken ct = default) => Db.Set<ApexPost.Infrastructure.Persistence.Entities.StockAdjustmentRecord>().AsNoTracking().AnyAsync(h => h.ProductId == productId, ct);
    public Task<bool> SkuExistsAsync(ApexPost.Domain.ValueObjects.Sku sku, Guid? excludingId, CancellationToken ct = default) => Db.Set<Product>().AsNoTracking().AnyAsync(p => p.Sku == sku && (excludingId == null || p.Id != excludingId), ct);
}

public sealed class EfTransactionRepository(ApexPostDbContext db) : EfRepository<Transaction>(db), ITransactionRepository
{
    protected override IQueryable<Transaction> Aggregate => Db.Set<Transaction>().Include(x => x.Lines);

    public Task<bool> HasProductHistoryAsync(Guid productId, CancellationToken ct = default) => Db.Set<TransactionLine>().AsNoTracking().AnyAsync(l => l.ProductId == productId, ct);
    public override Task SaveAsync(Transaction entity, CancellationToken cancellationToken = default)
    {
        Db.Entry(entity).Property("ModifiedAtUtc").CurrentValue = DateTime.UtcNow;
        return base.SaveAsync(entity, cancellationToken);
    }
}

public sealed class EfStockRepository(ApexPostDbContext db) : EfRepository<BranchStock>(db), IBranchStockRepository
{
    public async Task<IReadOnlyList<BranchStock>> ListByProductAsync(Guid productId, CancellationToken ct = default) => await Db.Set<BranchStock>().Where(s => s.ProductId == productId).ToArrayAsync(ct);
    public Task<BranchStock?> GetByBranchAndProductAsync(Guid branchId, Guid productId, CancellationToken cancellationToken = default) => Db.Set<BranchStock>().SingleOrDefaultAsync(x => x.BranchId == branchId && x.ProductId == productId, cancellationToken);
}

public sealed class EfPaymentRepository(ApexPostDbContext db) : IPaymentRepository
{
    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await db.Set<Payment>().AddAsync(payment, cancellationToken);
    }
}
