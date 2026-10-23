using ApexPost.Domain.Entities;

namespace ApexPost.Application.Abstractions.Services;

public interface IStaffManagementService
{
    Task<Staff> CreateAsync(Staff staff, CancellationToken cancellationToken = default);
    Task<Staff?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Staff>> ListByBranchAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
}
