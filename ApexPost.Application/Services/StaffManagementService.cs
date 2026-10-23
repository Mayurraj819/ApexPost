using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Domain.Entities;

namespace ApexPost.Application.Services;

public sealed class StaffManagementService : IStaffManagementService
{
    private readonly IStaffRepository _staff;

    public StaffManagementService(IStaffRepository staff) => _staff = staff;

    public async Task<Staff> CreateAsync(Staff staff, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(staff);
        await _staff.AddAsync(staff, cancellationToken);
        return staff;
    }

    public Task<Staff?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _staff.GetByIdAsync(id, cancellationToken);

    public async Task<IReadOnlyList<Staff>> ListByBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            throw new ArgumentException("Branch ID cannot be empty.", nameof(branchId));
        }

        return (await _staff.ListAsync(cancellationToken)).Where(item => item.BranchId == branchId).ToArray();
    }

    public Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default) => ChangeStateAsync(id, staff => staff.Deactivate(), cancellationToken);

    public Task ActivateAsync(Guid id, CancellationToken cancellationToken = default) => ChangeStateAsync(id, staff => staff.Activate(), cancellationToken);

    private async Task ChangeStateAsync(Guid id, Action<Staff> change, CancellationToken cancellationToken)
    {
        var staff = await _staff.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException($"Staff member '{id}' was not found.");
        change(staff);
        await _staff.SaveAsync(staff, cancellationToken);
    }
}
