using System.Net.Http;
using ApexPost.Courier.Offline.Abstractions;
using ApexPost.Courier.Offline.Models;
using ApexPost.Courier.Offline.Services;
using Moq;
namespace ApexPost.UnitTests;
// GitHub Copilot-assisted draft; human review recorded in COPILOT-LOG.md.
public class OutboxServiceTests
{
    [Theory]
    [InlineData(true, 1, 0, SyncStatus.Synced)]
    [InlineData(false, 0, 1, SyncStatus.Failed)]
    public async Task SyncPendingAsync_WhenUploaderReturnsResult_ReportsCorrectSummaryAndPersistsSyncState(
        bool uploadResult,
        int expectedSynced,
        int expectedFailed,
        SyncStatus expectedStatus)
    {
        // Arrange
        var originalScan = CreateScan(
            id: "scan::11111111-1111-4111-8111-111111111111",
            trackingNumber: "APX-10000001",
            scanType: ScanType.InTransit,
            courierId: "courier-42",
            location: "Seattle, WA",
            scannedAtUtc: new DateTimeOffset(2025, 02, 15, 08, 45, 00, TimeSpan.Zero),
            notes: "Fictional package scan for theory coverage");

        var repository = new Mock<IScanRepository>();
        repository.Setup(r => r.ListPending()).Returns(new[] { originalScan });
      
        var uploader = new Mock<IShipmentUploader>();
        uploader
            .Setup(u => u.UploadAsync(It.Is<ShipmentScan>(scan => scan.Id == originalScan.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        var service = new OutboxService(repository.Object, uploader.Object);

        // Act
        var summary = await service.SyncPendingAsync(CancellationToken.None);

        // Assert
        Assert.Equal(new SyncSummary(expectedSynced, expectedFailed), summary);

        uploader.Verify(
            u => u.UploadAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()),
            Times.Once);

        repository.Verify(
            r => r.Update(It.Is<ShipmentScan>(updated =>
                updated.Id == originalScan.Id &&
                updated.TrackingNumber == originalScan.TrackingNumber &&
                updated.ScanType == originalScan.ScanType &&
                updated.CourierId == originalScan.CourierId &&
                updated.Location == originalScan.Location &&
                updated.ScannedAtUtc == originalScan.ScannedAtUtc &&
                updated.Notes == originalScan.Notes &&
                updated.SyncStatus == expectedStatus)),
            Times.Once);
        repository.Verify(
  r => r.Update(It.IsAny<ShipmentScan>()),
  Times.Once);
    }

    [Fact]
    public async Task SyncPendingAsync_WhenUploaderThrowsHttpRequestException_MarksScanFailedAndReportsSummary()
    {
        // Arrange
        var originalScan = CreateScan(
            id: "scan::22222222-2222-4222-8222-222222222222",
            trackingNumber: "APX-10000002",
            scanType: ScanType.Delivered,
            courierId: "courier-77",
            location: "Denver, CO",
            scannedAtUtc: new DateTimeOffset(2025, 03, 10, 16, 05, 00, TimeSpan.Zero),
            notes: "Fictional last-mile delivery scan");

        var repository = new Mock<IScanRepository>();
        repository.Setup(r => r.ListPending()).Returns(new[] { originalScan });
      
        var uploader = new Mock<IShipmentUploader>();
        uploader
            .Setup(u => u.UploadAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Upload failed"));

        var service = new OutboxService(repository.Object, uploader.Object);

        // Act
        var summary = await service.SyncPendingAsync(CancellationToken.None);

        // Assert
        Assert.Equal(new SyncSummary(0, 1), summary);

        uploader.Verify(
            u => u.UploadAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()),
            Times.Once);

        repository.Verify(
            r => r.Update(It.Is<ShipmentScan>(updated =>
                updated.Id == originalScan.Id &&
                updated.TrackingNumber == originalScan.TrackingNumber &&
                updated.ScanType == originalScan.ScanType &&
                updated.CourierId == originalScan.CourierId &&
                updated.Location == originalScan.Location &&
                updated.ScannedAtUtc == originalScan.ScannedAtUtc &&
                updated.Notes == originalScan.Notes &&
                updated.SyncStatus == SyncStatus.Failed)),
            Times.Once);
        repository.Verify(
  r => r.Update(It.IsAny<ShipmentScan>()),
  Times.Once);
    }

    [Fact]
    public async Task SyncPendingAsync_WhenCancellationRequestedBeforeProcessing_ThrowsAndDoesNotUploadOrUpdate()
    {
        // Arrange
        var originalScan = CreateScan(
            id: "scan::33333333-3333-4333-8333-333333333333",
            trackingNumber: "APX-10000003",
            scanType: ScanType.Pickup,
            courierId: "courier-09",
            location: "Austin, TX",
            scannedAtUtc: new DateTimeOffset(2025, 04, 18, 11, 20, 00, TimeSpan.Zero),
            notes: "Fictional pickup scan before cancellation");

        var repository = new Mock<IScanRepository>();
        repository.Setup(r => r.ListPending()).Returns(new[] { originalScan });

        var uploader = new Mock<IShipmentUploader>();
       using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var service = new OutboxService(repository.Object, uploader.Object);

        // Act
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
      () => service.SyncPendingAsync(cancellationTokenSource.Token));
        // Assert
        Assert.Equal(cancellationTokenSource.Token, exception.CancellationToken);
        uploader.Verify(
            u => u.UploadAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(
            r => r.Update(It.IsAny<ShipmentScan>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncPendingAsync_WhenCancellationOccursDuringUpload_ThrowsAndDoesNotFailOrUpdateScan()
    {
        // Arrange
        var originalScan = CreateScan(
            id: "scan::44444444-4444-4444-8444-444444444444",
            trackingNumber: "APX-10000004",
            scanType: ScanType.Failed,
            courierId: "courier-18",
            location: "Chicago, IL",
            scannedAtUtc: new DateTimeOffset(2025, 05, 02, 07, 10, 00, TimeSpan.Zero),
            notes: "Fictional failed scan while upload is cancelled");

        var repository = new Mock<IScanRepository>();
        repository.Setup(r => r.ListPending()).Returns(new[] { originalScan });

       using var cancellationTokenSource = new CancellationTokenSource();
        var uploader = new Mock<IShipmentUploader>();
        uploader
     .Setup(u => u.UploadAsync(
         It.IsAny<ShipmentScan>(),
         It.IsAny<CancellationToken>()))
     .Returns((ShipmentScan _, CancellationToken token) =>
     {
         cancellationTokenSource.Cancel();
         return Task.FromCanceled<bool>(token);
     });

        var service = new OutboxService(repository.Object, uploader.Object);

        // Act
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
    () => service.SyncPendingAsync(cancellationTokenSource.Token));
        // Assert
        Assert.Equal(cancellationTokenSource.Token, exception.CancellationToken);
        uploader
    .Setup(u => u.UploadAsync(
        It.IsAny<ShipmentScan>(),
        It.IsAny<CancellationToken>()))
    .Returns((ShipmentScan _, CancellationToken token) =>
    {
        cancellationTokenSource.Cancel();
        return Task.FromCanceled<bool>(token);
    });
        uploader.Verify(
            u => u.UploadAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(
            r => r.Update(It.IsAny<ShipmentScan>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncPendingAsync_WhenNoPendingScans_ReturnsZeroSummaryWithoutUploadOrUpdate()
    {
        // Arrange
        var repository = new Mock<IScanRepository>();
        repository.Setup(r => r.ListPending()).Returns(Array.Empty<ShipmentScan>());

        var uploader = new Mock<IShipmentUploader>();
        var service = new OutboxService(repository.Object, uploader.Object);

        // Act
        var summary = await service.SyncPendingAsync(CancellationToken.None);

        // Assert
        Assert.Equal(new SyncSummary(0, 0), summary);

        uploader.Verify(
            u => u.UploadAsync(It.IsAny<ShipmentScan>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(
            r => r.Update(It.IsAny<ShipmentScan>()),
            Times.Never);
    }

    private static ShipmentScan CreateScan(
        string id,
        string trackingNumber,
        ScanType scanType,
        string courierId,
        string location,
        DateTimeOffset scannedAtUtc,
        string notes)
    {
        return new ShipmentScan
        {
            Id = id,
            TrackingNumber = trackingNumber,
            ScanType = scanType,
            CourierId = courierId,
            Location = location,
            ScannedAtUtc = scannedAtUtc,
            Notes = notes,
            SyncStatus = SyncStatus.Pending
        };
    }
}
