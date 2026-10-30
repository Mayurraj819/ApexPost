using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Entities;

namespace ApexPost.Infrastructure.Persistence;

public sealed class JsonBranchStockRepository : JsonRepository<BranchStock>, IBranchStockRepository
{
    public JsonBranchStockRepository(IJsonStore<BranchStock> store) : base(store)
    {
    }

    public async Task<BranchStock?> GetByBranchAndProductAsync(Guid branchId, Guid productId, CancellationToken cancellationToken = default)
    {
        var stocks = await ListAsync(cancellationToken);
        return stocks.SingleOrDefault(item => item.BranchId == branchId && item.ProductId == productId);
    }
}
