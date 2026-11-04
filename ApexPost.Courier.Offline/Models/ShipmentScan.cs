namespace ApexPost.Courier.Offline.Models;

public enum ScanType
{
    Pickup,
    InTransit,
    Delivered,
    Failed
}

public enum SyncStatus
{
    Pending,
    Synced,
    Failed
}

public sealed record ShipmentScan
{
    public string Id { get; init; } = $"scan::{Guid.NewGuid():D}";
    public required string TrackingNumber { get; init; }
    public required ScanType ScanType { get; init; }
    public required string CourierId { get; init; }
    public required string Location { get; init; }
    public DateTimeOffset ScannedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string Notes { get; init; } = string.Empty;
    public SyncStatus SyncStatus { get; init; } = SyncStatus.Pending;
}