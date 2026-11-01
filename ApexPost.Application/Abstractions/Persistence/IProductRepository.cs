using ApexPost.Domain.Entities;

namespace ApexPost.Application.Abstractions.Persistence;
public interface IProductRepository : IRepository<Product>
{
    Task<bool> HasStockAdjustmentHistoryAsync(Guid productId, CancellationToken ct = default);
    Task<bool> SkuExistsAsync(ApexPost.Domain.ValueObjects.Sku sku, Guid? excludingId, CancellationToken ct = default);
}
