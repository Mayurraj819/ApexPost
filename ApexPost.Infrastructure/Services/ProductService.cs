using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Domain.Entities;

namespace ApexPost.Infrastructure.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default) =>
        _repository.ListAsync(cancellationToken);
}
