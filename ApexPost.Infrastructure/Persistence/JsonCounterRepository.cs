using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Entities;

namespace ApexPost.Infrastructure.Persistence;

public sealed class JsonCounterRepository : JsonRepository<Counter>, ICounterRepository
{
    public JsonCounterRepository(IJsonStore<Counter> store) : base(store)
    {
    }
}
