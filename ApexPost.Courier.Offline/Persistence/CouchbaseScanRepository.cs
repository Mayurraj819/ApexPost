using ApexPost.Courier.Offline.Abstractions;
using ApexPost.Courier.Offline.Models;
using Couchbase.Lite;
using Couchbase.Lite.Query;

namespace ApexPost.Courier.Offline.Persistence;

public sealed class CouchbaseScanRepository : IScanRepository
{
    private readonly Collection _scans;

    public CouchbaseScanRepository(CourierDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _scans = database.Scans;
    }

    public ShipmentScan Create(ShipmentScan scan)
    {
        Validate(scan);

        var pending = scan with
        {
            ScannedAtUtc = scan.ScannedAtUtc.ToUniversalTime(),
            SyncStatus = SyncStatus.Pending,
            Notes = scan.Notes ?? string.Empty
        };

        using var document = new MutableDocument(pending.Id);
        ShipmentScanMapper.Write(document, pending);

        if (!_scans.Save(document, ConcurrencyControl.FailOnConflict))
        {
            throw new InvalidOperationException(
                $"Scan '{pending.Id}' already exists or changed.");
        }

        return pending;
    }

    public ShipmentScan? GetById(string id)
    {
        ValidateId(id);

        using var document = _scans.GetDocument(id);

        return document is null
            ? null
            : ShipmentScanMapper.Read(document);
    }

    public void Update(ShipmentScan scan)
    {
        Validate(scan);

        using var existing = _scans.GetDocument(scan.Id)
            ?? throw new KeyNotFoundException(
                $"Scan '{scan.Id}' was not found.");

        using var document = existing.ToMutable();
        ShipmentScanMapper.Write(document, scan);

        if (!_scans.Save(document, ConcurrencyControl.FailOnConflict))
        {
            throw new InvalidOperationException(
                "The scan changed during the update. Reload and retry.");
        }
    }

    public bool Delete(string id)
    {
        ValidateId(id);

        using var document = _scans.GetDocument(id);

        if (document is null)
            return false;

        if (!_scans.Delete(document, ConcurrencyControl.FailOnConflict))
        {
            throw new InvalidOperationException(
                "The scan changed during deletion. Reload and retry.");
        }

        return true;
    }

    public IReadOnlyList<ShipmentScan> ListAll()
    {
        return QueryScans("""
            SELECT META(s).id AS id
            FROM _ AS s
            ORDER BY s.scannedAtUtc, META(s).id
            """);
    }

    public IReadOnlyList<ShipmentScan> FindByTrackingNumber(
        string trackingNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        var parameters = new Parameters()
            .SetString("trackingNumber", trackingNumber);

        return QueryScans("""
            SELECT META(s).id AS id
            FROM _ AS s
            WHERE s.trackingNumber = $trackingNumber
            ORDER BY s.scannedAtUtc, META(s).id
            """, parameters);
    }

    public IReadOnlyList<ShipmentScan> ListPending()
    {
        var parameters = new Parameters()
            .SetString("status", SyncStatus.Pending.ToString());

        return QueryScans("""
            SELECT META(s).id AS id
            FROM _ AS s
            WHERE s.syncStatus = $status
            ORDER BY s.scannedAtUtc, META(s).id
            """, parameters);
    }

    public IReadOnlyDictionary<ScanType, long> CountByScanType(
        string courierId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(courierId);

        using var query = _scans.CreateQuery("""
            SELECT s.scanType AS scanType, COUNT(*) AS scanCount
            FROM _ AS s
            WHERE s.courierId = $courierId
            GROUP BY s.scanType
            """);

        query.Parameters = new Parameters()
            .SetString("courierId", courierId);

        var counts = Enum.GetValues<ScanType>()
            .ToDictionary(type => type, _ => 0L);

        using var results = query.Execute();

        foreach (var row in results)
        {
            var value = row.GetString("scanType");

            if (!Enum.TryParse<ScanType>(value, out var type)
                || !Enum.IsDefined(type))
            {
                throw new InvalidDataException(
                    $"Invalid stored scan type: '{value}'.");
            }

            counts[type] = row.GetLong("scanCount");
        }

        return counts;
    }

    private IReadOnlyList<ShipmentScan> QueryScans(
        string sql,
        Parameters? parameters = null)
    {
        using var query = _scans.CreateQuery(sql);

        if (parameters is not null)
            query.Parameters = parameters;

        using var results = query.Execute();
        var scans = new List<ShipmentScan>();

        foreach (var row in results)
        {
            var id = row.GetString("id")
                ?? throw new InvalidDataException(
                    "Query result has no document ID.");

            var scan = GetById(id);

            if (scan is not null)
                scans.Add(scan);
        }

        return scans;
    }

    private static void Validate(ShipmentScan scan)
    {
        ArgumentNullException.ThrowIfNull(scan);
        ValidateId(scan.Id);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            scan.TrackingNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(scan.CourierId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scan.Location);

        if (!Enum.IsDefined(scan.ScanType)
            || !Enum.IsDefined(scan.SyncStatus))
        {
            throw new ArgumentException(
                "Scan type or sync status is invalid.");
        }

        if (scan.ScannedAtUtc == default)
        {
            throw new ArgumentException(
                "A scan timestamp is required.");
        }
    }

    private static void ValidateId(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!id.StartsWith("scan::", StringComparison.Ordinal)
            || !Guid.TryParseExact(id[6..], "D", out _))
        {
            throw new ArgumentException(
                "Scan ID must use the format scan::{Guid}.",
                nameof(id));
        }
    }
}