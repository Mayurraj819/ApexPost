using ApexPost.Courier.Offline.Abstractions;
using ApexPost.Courier.Offline.Models;
using ApexPost.Courier.Offline.Persistence;
using ApexPost.Courier.Offline.Services;
using Terminal = System.Console;

using var database = new CourierDatabase();
IScanRepository repository = new CouchbaseScanRepository(database);

database.StartChangeListener(message => Terminal.WriteLine(message));

Terminal.WriteLine("ApexPost Courier — Offline Console");
Terminal.WriteLine("Use fictional shipment and courier details only.");

while (true)
{
    Terminal.WriteLine("""
        
        1. Add Scan
        2. Find by Tracking Number
        3. List Pending
        4. Simulate Sync
        5. Delete Scan
        6. Exit
        7. Counts by Courier
        8. List All Scans
        """);

    Terminal.Write("Choose: ");
    var choice = Terminal.ReadLine()?.Trim();

    if (choice is null or "6")
        break;

    try
    {
        switch (choice)
        {
            case "1":
                AddScan(repository);
                break;

            case "2":
                PrintScans(repository.FindByTrackingNumber(
                    ReadRequired("Tracking number")));
                break;

            case "3":
                PrintScans(repository.ListPending());
                break;

            case "4":
                Terminal.Write("Simulate upload failure? (y/N): ");
                var simulateFailure = string.Equals(
                    Terminal.ReadLine()?.Trim(),
                    "y",
                    StringComparison.OrdinalIgnoreCase);

                var uploader = new FakeShipmentUploader(simulateFailure);
                var outbox = new OutboxService(repository, uploader);
                var summary = await outbox.SyncPendingAsync();

                Terminal.WriteLine(
                    $"Sync finished: {summary.Synced} synced, " +
                    $"{summary.Failed} failed.");
                break;

            case "5":
                var deleted = repository.Delete(
                    ReadRequired("Scan ID (scan::...)"));

                Terminal.WriteLine(
                    deleted ? "Scan deleted." : "Scan not found.");
                break;

            case "7":
                var counts = repository.CountByScanType(
                    ReadRequired("Courier ID"));

                foreach (var entry in counts)
                    Terminal.WriteLine($"{entry.Key}: {entry.Value}");
                break;

            case "8":
                PrintScans(repository.ListAll());
                break;

            default:
                Terminal.WriteLine("Choose a number from 1 to 8.");
                break;
        }
    }
    catch (Exception ex)
    {
        Terminal.WriteLine(
            $"Error ({ex.GetType().Name}): {ex.Message}");
    }
}

Terminal.WriteLine("Closing the local database.");

static void AddScan(IScanRepository repository)
{
    var trackingNumber = ReadRequired("Tracking number");
    var courierId = ReadRequired("Courier ID");
    var location = ReadRequired("Location");

    Terminal.Write("Scan type (Pickup/InTransit/Delivered/Failed): ");
    var typeInput = Terminal.ReadLine()?.Trim();

    if (!Enum.TryParse<ScanType>(
            typeInput, ignoreCase: true, out var scanType)
        || !Enum.IsDefined(scanType))
    {
        throw new ArgumentException("Choose a valid scan type.");
    }

    Terminal.Write("Notes (optional): ");
    var notes = Terminal.ReadLine() ?? string.Empty;

    var saved = repository.Create(new ShipmentScan
    {
        TrackingNumber = trackingNumber,
        CourierId = courierId,
        Location = location,
        ScanType = scanType,
        Notes = notes
    });

    Terminal.WriteLine(
        $"Saved locally: {saved.Id} | Status: {saved.SyncStatus}");
}

static string ReadRequired(string label)
{
    Terminal.Write($"{label}: ");
    var value = Terminal.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(value))
        throw new ArgumentException($"{label} is required.");

    return value;
}

static void PrintScans(IEnumerable<ShipmentScan> scans)
{
    var items = scans.ToArray();

    if (items.Length == 0)
    {
        Terminal.WriteLine("No scans found.");
        return;
    }

    foreach (var scan in items)
    {
        Terminal.WriteLine(
            $"""
            
            ID:       {scan.Id}
            Tracking: {scan.TrackingNumber}
            Type:     {scan.ScanType}
            Courier:  {scan.CourierId}
            Location: {scan.Location}
            UTC time: {scan.ScannedAtUtc:O}
            Status:   {scan.SyncStatus}
            Notes:    {scan.Notes}
            """);
    }

    Terminal.WriteLine($"Total: {items.Length}");
}