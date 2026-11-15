using ApexPost.Application.Delivery;
using ApexPost.Domain.Delivery;
using ApexPost.Infrastructure.Delivery;
using ApexPost.Infrastructure.Persistence;
using ApexPost.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Api.Tests;
// Missing SQL infrastructure fails explicitly instead of silently skipping.
public sealed class DeliverySqlFactAttribute : FactAttribute;

[Trait("Category", "SqlServer")]
public sealed class SqlDeliveryTests
{
    [DeliverySqlFact]
    public async Task Persistent_crud_workflow_dedup_and_concurrent_uploads()
    {
        await using var database = await SqlTestDatabase.CreateAsync();
        ApexPostDbContext Context() => database.CreateContext();
        await using var db = Context();
        var service = new ShipmentApiService(db, new EfUnitOfWork(db));
        var d = new ShipmentDetails(1, "Fictional", "+919876543210", "Demo street", "Pune");
        await service.CreateAsync("APX-12345678", d, default);
        Assert.Equal("Pune", (await service.GetAsync("APX-12345678", default)).City);
        await service.ReplaceAsync("APX-12345678", d with { City = "Mumbai" }, default);
        Assert.Single((await service.ListAsync(1, 20, DeliveryStatus.Created, "Mumbai", default)).Items);
        var scan = new ScanInput("scan::" + Guid.NewGuid(), "APX-12345678", CourierScanType.Pickup, "COURIER-01", "Demo", new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero), "");
        var first = await service.UploadAsync(scan.TrackingNumber, [scan, scan], default);
        Assert.Equal(1, first.Accepted);
        Assert.Equal(1, first.DuplicatesSkipped);
        Assert.Equal(1, (await service.UploadAsync(scan.TrackingNumber, [scan], default)).DuplicatesSkipped);
        var conflict = await service.UploadAsync(scan.TrackingNumber, [scan with { Location = "Different" }], default);
        Assert.Equal(1, conflict.Rejected);
        var fresh = scan with
        {
            Id = "scan::" + Guid.NewGuid(),
            ScanType = CourierScanType.InTransit
        };
        async Task<ScanUploadResult> Concurrent()
        {
            await using var other = Context();
            return await new ShipmentApiService(other, new EfUnitOfWork(other)).UploadAsync(scan.TrackingNumber, [fresh], default);
        }

        var results = await Task.WhenAll(Concurrent(), Concurrent());
        Assert.Equal(1, results.Sum(x => x.Accepted));
        Assert.Equal(1, results.Sum(x => x.DuplicatesSkipped));
        Assert.Equal(2, (await service.ScansAsync(scan.TrackingNumber, default)).Count);
        foreach (var status in new[]
        {
            DeliveryStatus.PickedUp,
            DeliveryStatus.InTransit,
            DeliveryStatus.OutForDelivery,
            DeliveryStatus.Delivered
        }

        )
            await service.ChangeStatusAsync(scan.TrackingNumber, status, default);
        await Assert.ThrowsAsync<DeliveryConflictException>(() => service.DeleteAsync(scan.TrackingNumber, default));
        await service.CreateAsync("APX-87654321", d, default);
        await service.DeleteAsync("APX-87654321", default);
        await Assert.ThrowsAsync<ShipmentNotFoundException>(() => service.GetAsync("APX-87654321", default));
    }
}
