using ApexPost.Courier.Offline.Models;

namespace ApexPost.Courier.Offline.Abstractions;

public interface IShipmentUploader
{
    Task<bool> UploadAsync(
        ShipmentScan scan,
        CancellationToken cancellationToken = default);
}