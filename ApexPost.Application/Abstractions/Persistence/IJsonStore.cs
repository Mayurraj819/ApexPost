namespace ApexPost.Application.Abstractions.Persistence;

public interface IJsonStore<TEntity> where TEntity : class
{
    Task<IReadOnlyList<TEntity>> ReadAsync(CancellationToken cancellationToken = default);
    Task WriteAsync(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken = default);
}
