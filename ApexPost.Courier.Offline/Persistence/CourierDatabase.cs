using Couchbase.Lite;
using Couchbase.Lite.Query;

namespace ApexPost.Courier.Offline.Persistence;

public sealed class CourierDatabase : IDisposable
{
    private readonly Database _database;
    private readonly object _listenerGate = new();
    private ListenerToken? _listenerToken;
    private bool _disposed;

    public Collection Scans { get; }

    public CourierDatabase(string? directory = null)
    {
        directory ??= Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "ApexPost",
            "Courier");

        Directory.CreateDirectory(directory);

        _database = new Database(
            "apexpost_courier",
            new DatabaseConfiguration
            {
                Directory = directory
            });

        try
        {
            Scans = _database.GetDefaultCollection()
                ?? throw new InvalidOperationException(
                    "The default collection is unavailable.");

            Scans.CreateIndex(
                "idx_trackingNumber",
                new ValueIndexConfiguration("trackingNumber"));

            Scans.CreateIndex(
                "idx_syncStatus",
                new ValueIndexConfiguration("syncStatus"));
        }
        catch
        {
            _database.Dispose();
            throw;
        }
    }

    public void StartChangeListener(Action<string> log)
    {
        ArgumentNullException.ThrowIfNull(log);

        lock (_listenerGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_listenerToken.HasValue)
            {
                throw new InvalidOperationException(
                    "The change listener is already registered.");
            }

            _listenerToken = Scans.AddChangeListener((_, args) =>
            {
                lock (_listenerGate)
                {
                    if (_disposed)
                        return;

                    try
                    {
                        var ids = string.Join(", ", args.DocumentIDs);

                        log(
                            $"[Listener] Changed IDs: {ids}" +
                            $" | Pending scans: {CountPending()}");
                    }
                    catch (CouchbaseException ex)
                    {
                        log($"[Listener] Database error: {ex.Message}");
                    }
                }
            });

            log($"[Listener] Started | Pending scans: {CountPending()}");
        }
    }

    private long CountPending()
    {
        using var query = Scans.CreateQuery("""
            SELECT COUNT(*) AS pendingCount
            FROM _
            WHERE syncStatus = $status
            """);

        query.Parameters = new Parameters()
            .SetString("status", "Pending");

        using var results = query.Execute();

        return results.FirstOrDefault()?.GetLong("pendingCount") ?? 0;
    }

    public void Dispose()
    {
        ListenerToken? token;

        lock (_listenerGate)
        {
            if (_disposed)
                return;

            _disposed = true;
            token = _listenerToken;
            _listenerToken = null;
        }

        // Remove outside the lock so callbacks can finish safely.
        try
        {
            token?.Remove();
        }
        finally
        {
            _database.Dispose();
        }
    }
}