using System.Globalization;
using ApexPost.Courier.Offline.Models;
using Couchbase.Lite;

namespace ApexPost.Courier.Offline.Persistence;

public static class ShipmentScanMapper
{
    public static void Write(
        MutableDocument document,
        ShipmentScan scan)
    {
        document.SetString("trackingNumber", scan.TrackingNumber);
        document.SetString("scanType", scan.ScanType.ToString());
        document.SetString("courierId", scan.CourierId);
        document.SetString("location", scan.Location);
        document.SetString(
            "scannedAtUtc",
            scan.ScannedAtUtc.ToUniversalTime()
                .ToString("O", CultureInfo.InvariantCulture));
        document.SetString("notes", scan.Notes);
        document.SetString("syncStatus", scan.SyncStatus.ToString());
    }

    public static ShipmentScan Read(Document document)
    {
        return new ShipmentScan
        {
            Id = document.Id,
            TrackingNumber = Required(document, "trackingNumber"),
            ScanType = ParseEnum<ScanType>(
                Required(document, "scanType")),
            CourierId = Required(document, "courierId"),
            Location = Required(document, "location"),
            ScannedAtUtc = DateTimeOffset.ParseExact(
                Required(document, "scannedAtUtc"),
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None),
            Notes = document.GetString("notes") ?? string.Empty,
            SyncStatus = ParseEnum<SyncStatus>(
                Required(document, "syncStatus"))
        };
    }

    private static string Required(Document document, string field)
    {
        var value = document.GetString(field);

        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidDataException(
                $"Document '{document.Id}' has no valid '{field}'.");
    }

    private static T ParseEnum<T>(string value)
        where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new InvalidDataException(
            $"Invalid {typeof(T).Name} value: '{value}'.");
    }
}