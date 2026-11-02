using ApexPost.Application.Contracts;

namespace ApexPost.Application.Abstractions.Services;
public interface ICatalogCommands
{
    Task SaveAsync(SaveProductCommand command, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task RemoveEmptyStockAsync(Guid branchId, Guid productId, CancellationToken ct);
    Task SetStockAsync(SetStockCommand command, CancellationToken ct);
}
