using ApexPost.Domain.Entities;

namespace ApexPost.Application.Abstractions.Persistence;

public interface IBranchStockRepository : IRepository<BranchStock>
{
    Task<BranchStock?> GetByBranchAndProductAsync(Guid branchId, Guid productId, CancellationToken cancellationToken = default);
}
