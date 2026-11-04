using ApexPost.Client;
using Microsoft.Extensions.DependencyInjection;

namespace ApexPost.Api.Tests;
public sealed class GeneratedClientTests
{
    [Fact]
    public async Task Generated_sdk_creates_reads_uploads_and_throws_typed_404()
    {
        await using var factory = new ApiFactory();
        var services = new ServiceCollection();
        services.AddHttpClient<IApexPostClient, ApexPostClient>(c => c.BaseAddress = new Uri("https://localhost")).ConfigurePrimaryHttpMessageHandler(() => factory.Server.CreateHandler());
        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IApexPostClient>();
        await client.CreateShipmentV2Async(new CreateShipmentRequest { TrackingNumber = "APX-12345678", WeightKg = 1.25, RecipientName = "Fictional", RecipientPhone = "+919876543210", Address = "Demo street", City = "Pune" });
        Assert.Equal("Fictional", (await client.GetShipmentV2Async("APX-12345678")).Recipient.Name);
        var scans = new[]
        {
            "Pickup",
            "InTransit"
        }.Select((type, i) => new ScanUploadRequest { Id = "scan::" + Guid.NewGuid(), TrackingNumber = "APX-12345678", ScanType = type, CourierId = "COURIER-01", Location = "Demo depot", ScannedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2 + i), Notes = "" }).ToArray();
        Assert.Equal(2, (await client.UploadShipmentScansV2Async("APX-12345678", scans)).Accepted);
        Assert.Equal(2, (await client.UploadShipmentScansV2Async("APX-12345678", scans)).DuplicatesSkipped);
        var fetched = await client.GetShipmentV2Async("APX-12345678");
        Assert.Equal("InTransit", fetched.LatestScan!.ScanType);
        var missing = await Assert.ThrowsAsync<ApiException<ProblemDetails>>(() => client.GetShipmentV2Async("APX-00000000"));
        Assert.Equal(404, missing.StatusCode);
        var page = await client.ListShipmentsV2Async(page: 1, pageSize: 20);
        Assert.Single(page.Items);
    }
}
