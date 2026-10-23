using ApexPost.Domain.Entities;

namespace ApexPost.Application.Abstractions.Services;

public interface IBranchManagementService
{
    Task<Branch> CreateAsync(Branch branch, CancellationToken cancellationToken = default);
    Task<Branch?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Branch>> ListAsync(CancellationToken cancellationToken = default);
    Task CloseAsync(Guid id, CancellationToken cancellationToken = default);
    Task ReopenAsync(Guid id, CancellationToken cancellationToken = default);
}
