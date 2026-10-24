namespace ApexPost.Application.Abstractions.Persistence;

public interface IAuditStore
{
    Task AppendAsync(object entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<object>> ReadAsync(CancellationToken cancellationToken = default);
}
