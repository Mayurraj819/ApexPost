using System.Text.Json;
using ApexPost.Application.Abstractions.Persistence;

namespace ApexPost.Infrastructure.Persistence;

public sealed class FileJsonStore<TEntity> : IJsonStore<TEntity> where TEntity : class
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileJsonStore(string filePath, JsonSerializerOptions? serializerOptions = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A JSON file path is required.", nameof(filePath));
        }

        _filePath = filePath;
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
    }

    public async Task<IReadOnlyList<TEntity>> ReadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_filePath))
            {
                return [];
            }

            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<List<TEntity>>(stream, _serializerOptions, cancellationToken) ?? [];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task WriteAsync(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, entities, _serializerOptions, cancellationToken);
            }

            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }
}
