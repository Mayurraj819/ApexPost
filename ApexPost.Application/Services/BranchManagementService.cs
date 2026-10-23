using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Domain.Entities;

namespace ApexPost.Application.Services;

public sealed class BranchManagementService : IBranchManagementService
{
    private readonly IBranchRepository _branches;

    public BranchManagementService(IBranchRepository branches) => _branches = branches;

    public async Task<Branch> CreateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(branch);
        await _branches.AddAsync(branch, cancellationToken);
        return branch;
    }

    public Task<Branch?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _branches.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Branch>> ListAsync(CancellationToken cancellationToken = default) =>
        _branches.ListAsync(cancellationToken);

    public Task CloseAsync(Guid id, CancellationToken cancellationToken = default) => ChangeStateAsync(id, branch => branch.Close(), cancellationToken);

    public Task ReopenAsync(Guid id, CancellationToken cancellationToken = default) => ChangeStateAsync(id, branch => branch.Reopen(), cancellationToken);

    private async Task ChangeStateAsync(Guid id, Action<Branch> change, CancellationToken cancellationToken)
    {
        var branch = await _branches.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException($"Branch '{id}' was not found.");
        change(branch);
        await _branches.SaveAsync(branch, cancellationToken);
    }
}
