using ApexPost.Domain.Entities;

namespace ApexPost.Application.Abstractions.Services;

public interface IInventoryService
{
    Task<BranchStock?> GetAsync(Guid branchId, Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BranchStock>> ListByBranchAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<BranchStock> AdjustAsync(Guid branchId, Guid productId, int delta, CancellationToken cancellationToken = default);
}
