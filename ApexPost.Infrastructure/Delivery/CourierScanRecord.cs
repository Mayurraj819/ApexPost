using ApexPost.Domain.Delivery;

namespace ApexPost.Infrastructure.Delivery;
public sealed class CourierScanRecord
{
    public string Id { get; set; } = "";
    public Guid ShipmentId { get; set; }
    public CourierScanType ScanType { get; set; }
    public string CourierId { get; set; } = "";
    public string Location { get; set; } = "";
    public DateTimeOffset ScannedAtUtc { get; set; }
    public string Notes { get; set; } = "";
}
