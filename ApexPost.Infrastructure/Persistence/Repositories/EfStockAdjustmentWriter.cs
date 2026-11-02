using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Infrastructure.Persistence.Entities;

namespace ApexPost.Infrastructure.Persistence.Repositories;
public sealed class EfStockAdjustmentWriter(ApexPostDbContext db) : IStockAdjustmentWriter
{
    public async Task StageAsync(Guid branchId, Guid productId, int delta, string reason, string actor, CancellationToken ct)
    {
        if (delta != 0)
            await db.Set<StockAdjustmentRecord>().AddAsync(new() { Id = Guid.NewGuid(), BranchId = branchId, ProductId = productId, Delta = delta, Reason = reason, ChangedBy = actor }, ct);
    }
}
