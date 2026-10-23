using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Common;

namespace ApexPost.Infrastructure.Persistence;

public class JsonRepository<TEntity> : IRepository<TEntity> where TEntity : Entity
{
    private readonly IJsonStore<TEntity> _store;

    public JsonRepository(IJsonStore<TEntity> store)
    {
        _store = store;
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        (await _store.ReadAsync(cancellationToken)).SingleOrDefault(entity => entity.Id == id);

    public Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
        _store.ReadAsync(cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var entities = (await _store.ReadAsync(cancellationToken)).ToList();
        if (entities.Any(existing => existing.Id == entity.Id))
        {
            throw new InvalidOperationException($"An entity with ID '{entity.Id}' already exists.");
        }

        entities.Add(entity);
        await _store.WriteAsync(entities, cancellationToken);
    }

    public async Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var entities = (await _store.ReadAsync(cancellationToken)).ToList();
        var index = entities.FindIndex(existing => existing.Id == entity.Id);
        if (index < 0)
        {
            throw new KeyNotFoundException($"An entity with ID '{entity.Id}' was not found.");
        }

        entities[index] = entity;
        await _store.WriteAsync(entities, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entities = (await _store.ReadAsync(cancellationToken)).ToList();
        if (entities.RemoveAll(entity => entity.Id == id) > 0)
        {
            await _store.WriteAsync(entities, cancellationToken);
        }
    }
}
