using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Entities;

namespace ApexPost.Infrastructure.Persistence;

public sealed class JsonStaffRepository : JsonRepository<Staff>, IStaffRepository
{
    public JsonStaffRepository(IJsonStore<Staff> store) : base(store)
    {
    }
}
