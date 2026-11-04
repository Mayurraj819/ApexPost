using ApexPost.Domain.Delivery;

namespace ApexPost.Application.Delivery;
public sealed record ShipmentDetails(decimal WeightKg, string RecipientName, string RecipientPhone, string Address, string City);
public sealed record ScanInput(string Id, string TrackingNumber, CourierScanType ScanType, string CourierId, string Location, DateTimeOffset ScannedAtUtc, string Notes);
public sealed record ScanView(string Id, string TrackingNumber, CourierScanType ScanType, string CourierId, string Location, DateTimeOffset ScannedAtUtc, string Notes);
public sealed record ShipmentView(string TrackingNumber, decimal WeightKg, string RecipientName, string RecipientPhone, string Address, string City, DeliveryStatus Status, ScanView? LatestScan);
public sealed record ShipmentPage(IReadOnlyList<ShipmentView> Items, int Page, int PageSize, int TotalCount);
public sealed record ScanRejection(string Id, string Reason);
public sealed record ScanUploadResult(int Accepted, int DuplicatesSkipped, int Rejected, IReadOnlyList<string> AcceptedScanIds, IReadOnlyList<string> DuplicateScanIds, IReadOnlyList<ScanRejection> Rejections);
public sealed class ShipmentNotFoundException(string tracking) : Exception($"Shipment '{tracking}' was not found.");
public interface IShipmentApiService
{
    Task<ShipmentPage> ListAsync(int page, int pageSize, DeliveryStatus? status, string? city, CancellationToken ct);
    Task<ShipmentView> GetAsync(string tracking, CancellationToken ct);
    Task<ShipmentView> CreateAsync(string tracking, ShipmentDetails details, CancellationToken ct);
    Task<ShipmentView> ReplaceAsync(string tracking, ShipmentDetails details, CancellationToken ct);
    Task<ShipmentView> ChangeStatusAsync(string tracking, DeliveryStatus status, CancellationToken ct);
    Task DeleteAsync(string tracking, CancellationToken ct);
    Task<ScanUploadResult> UploadAsync(string tracking, IReadOnlyList<ScanInput> scans, CancellationToken ct);
    Task<IReadOnlyList<ScanView>> ScansAsync(string tracking, CancellationToken ct);
}
