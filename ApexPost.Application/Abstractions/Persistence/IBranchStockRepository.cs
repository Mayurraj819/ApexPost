using ApexPost.Domain.Entities;

namespace ApexPost.Application.Abstractions.Persistence;
public interface IBranchStockRepository : IRepository<BranchStock>
{
    Task<IReadOnlyList<BranchStock>> ListByProductAsync(Guid productId, CancellationToken ct = default);
    Task<BranchStock?> GetByBranchAndProductAsync(Guid branchId, Guid productId, CancellationToken cancellationToken = default);
}
