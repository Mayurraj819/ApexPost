using System.Text.Json;
using ApexPost.Application.Abstractions.Persistence;

namespace ApexPost.Infrastructure.Persistence;

public sealed class JsonAuditStore : IAuditStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public JsonAuditStore(string dataDirectory)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new ArgumentException("A data directory is required.", nameof(dataDirectory));
        }

        _filePath = Path.Combine(dataDirectory, "audit.jsonl");
    }

    public async Task AppendAsync(object entry, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            await using var stream = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            await JsonSerializer.SerializeAsync(stream, entry, entry.GetType(), _serializerOptions, cancellationToken);
            await stream.WriteAsync(new byte[] { (byte)'\n' }, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<object>> ReadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_filePath))
            {
                return Array.Empty<object>();
            }

            var lines = await File.ReadAllLinesAsync(_filePath, cancellationToken);
            var results = new List<object>();
            foreach (var line in lines.Where(static value => !string.IsNullOrWhiteSpace(value)))
            {
                if (JsonSerializer.Deserialize<object>(line, _serializerOptions) is { } item)
                {
                    results.Add(item);
                }
            }

            return results;
        }
        finally
        {
            _gate.Release();
        }
    }
}
