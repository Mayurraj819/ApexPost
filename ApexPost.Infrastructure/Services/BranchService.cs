using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Domain.Entities;

namespace ApexPost.Infrastructure.Services;

public sealed class BranchService : IBranchService
{
    private readonly IBranchRepository _repository;

    public BranchService(IBranchRepository repository)
    {
        _repository = repository;
    }

    public Task<Branch?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Branch>> ListAsync(CancellationToken cancellationToken = default) =>
        _repository.ListAsync(cancellationToken);
}
