using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Entities;

namespace ApexPost.Infrastructure.Persistence;

public sealed class JsonBranchRepository : JsonRepository<Branch>, IBranchRepository
{
    public JsonBranchRepository(IJsonStore<Branch> store) : base(store)
    {
    }
}
