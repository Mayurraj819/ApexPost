using ApexPost.Domain.Entities;

namespace ApexPost.Application.Abstractions.Services;

public interface IBranchService
{
    Task<Branch?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Branch>> ListAsync(CancellationToken cancellationToken = default);
}
