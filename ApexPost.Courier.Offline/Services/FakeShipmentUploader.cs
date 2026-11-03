using ApexPost.Courier.Offline.Abstractions;
using ApexPost.Courier.Offline.Models;

namespace ApexPost.Courier.Offline.Services;

public sealed class FakeShipmentUploader(
    bool simulateFailure = false) : IShipmentUploader
{
    public Task<bool> UploadAsync(
        ShipmentScan scan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scan);
        cancellationToken.ThrowIfCancellationRequested();

        // Simulate a server response without making a network call.
        return Task.FromResult(!simulateFailure);
    }
}