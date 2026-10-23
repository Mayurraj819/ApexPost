using ApexPost.Domain.Entities;

namespace ApexPost.Application.Abstractions.Services;

public interface IProductService
{
    Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default);
}
