using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Entities;

namespace ApexPost.Infrastructure.Persistence;

public sealed class JsonProductRepository : JsonRepository<Product>, IProductRepository
{
    public JsonProductRepository(IJsonStore<Product> store) : base(store)
    {
    }
}
