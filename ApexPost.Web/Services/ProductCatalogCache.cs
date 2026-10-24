using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Entities;

namespace ApexPost.Web.Services;

public interface IProductCatalogCache
{
    ValueTask<IReadOnlyList<Product>> GetAsync(CancellationToken cancellationToken = default);
}

public sealed class ProductCatalogCache : IProductCatalogCache
{
    private readonly IProductRepository _repository;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IReadOnlyList<Product>? _cachedProducts;
    private DateTimeOffset _cachedUntil = DateTimeOffset.MinValue;

    public ProductCatalogCache(IProductRepository repository) => _repository = repository;

    public async ValueTask<IReadOnlyList<Product>> GetAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        if (_cachedProducts is not null && now < _cachedUntil)
        {
            return _cachedProducts;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_cachedProducts is not null && now < _cachedUntil)
            {
                return _cachedProducts;
            }

            var products = await _repository.ListAsync(cancellationToken);
            _cachedProducts = products;
            _cachedUntil = now.AddMinutes(5);
            return products;
        }
        finally
        {
            _gate.Release();
        }
    }
}
