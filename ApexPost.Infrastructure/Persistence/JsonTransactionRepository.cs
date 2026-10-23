using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Entities;

namespace ApexPost.Infrastructure.Persistence;

public sealed class JsonTransactionRepository : JsonRepository<Transaction>, ITransactionRepository
{
    public JsonTransactionRepository(IJsonStore<Transaction> store) : base(store)
    {
    }
}
