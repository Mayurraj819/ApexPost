using System.Net;
using System.Net.Http.Json;
using ApexPost.Api.Contracts;
using ApexPost.Application.Delivery;
using ApexPost.Domain.Delivery;
using ApexPost.Infrastructure.Delivery;
using ApexPost.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ApexPost.IntegrationTests.Api;

public sealed class ShipmentApiDatabaseTests
{
    private const string Tracking = "APX-10000001";
    private const string Route = "/api/v2/shipments/" + Tracking;

    [Fact]
    public async Task CreateUploadAndReplay_RealSql_PersistsEachScanOnceAndRejectsChangedContent()
    {
        await using var database = await SqlTestDatabase.CreateAsync();
        await using var factory = new SqlApiFactory(database.ConnectionString);
        using var client = factory.CreateAuthenticatedClient();
        using (var scope = factory.Services.CreateScope())
            Assert.IsType<ShipmentApiService>(scope.ServiceProvider.GetRequiredService<IShipmentApiService>());

        var created = await CreateShipmentAsync(client);
        Assert.Equal(1.25m, created.WeightKg);
        var scans = new[]
        {
            Scan("scan::11111111-1111-4111-8111-111111111111", "Pickup", 8),
            Scan("scan::22222222-2222-4222-8222-222222222222", "InTransit", 9)
        };

        var first = await UploadAsync(client, scans);
        Assert.Equal((2, 0, 0), (first.Accepted, first.DuplicatesSkipped, first.Rejected));
        Assert.Equal(scans.Select(s => s.Id).Order(), first.AcceptedScanIds.Order());

        var replay = await UploadAsync(client, scans);
        Assert.Equal((0, 2, 0), (replay.Accepted, replay.DuplicatesSkipped, replay.Rejected));
        Assert.Equal(scans.Select(s => s.Id).Order(), replay.DuplicateScanIds.Order());

        scans[0].Notes = "Changed content must not overwrite the original";
        var conflict = await UploadAsync(client, [scans[0]]);
        Assert.Equal((0, 0, 1), (conflict.Accepted, conflict.DuplicatesSkipped, conflict.Rejected));
        Assert.Equal(scans[0].Id, Assert.Single(conflict.Rejections).Id);

        // A fresh context checks actual persisted state, not the service's change tracker.
        await using var verification = database.CreateContext();
        var shipment = await verification.Set<TrackedShipment>().AsNoTracking().SingleAsync();
        var stored = await verification.Set<CourierScanRecord>().AsNoTracking()
            .OrderBy(s => s.ScannedAtUtc).ToArrayAsync();
        Assert.Equal(Tracking, shipment.TrackingNumber);
        Assert.Equal(DeliveryStatus.Created, shipment.Status); // Uploading history does not advance status.
        Assert.Equal(2, stored.Length);
        Assert.All(stored, s => Assert.Equal(shipment.Id, s.ShipmentId));
        Assert.Equal("Original fictional scan", stored[0].Notes);
        Assert.Equal(CourierScanType.InTransit, stored[1].ScanType);

        var fetched = await client.GetFromJsonAsync<ShipmentV2>(Route);
        Assert.NotNull(fetched);
        Assert.Equal("InTransit", fetched.LatestScan?.ScanType);
    }

    [Fact]
    public async Task Replace_DeliveredShipment_Returns409AndPreservesAllPersistedDetails()
    {
        await using var database = await SqlTestDatabase.CreateAsync();
        await using var factory = new SqlApiFactory(database.ConnectionString);
        using var client = factory.CreateAuthenticatedClient();
        var original = await CreateShipmentAsync(client);
        foreach (var status in new[] { "PickedUp", "InTransit", "OutForDelivery", "Delivered" })
        {
            using var transition = await client.PatchAsJsonAsync(Route + "/status", new UpdateStatusRequest { Status = status });
            Assert.Equal(HttpStatusCode.OK, transition.StatusCode);
        }

        using var response = await client.PutAsJsonAsync(Route, new UpdateShipmentRequest
        {
            WeightKg = 2.5m, RecipientName = "Other Fictional Recipient", RecipientPhone = "+919876543211",
            Address = "99 Fictional Road", City = "Mumbai"
        });
        await AssertProblemAsync(response, HttpStatusCode.Conflict);

        await using var verification = database.CreateContext();
        var stored = await verification.Set<TrackedShipment>().AsNoTracking().SingleAsync();
        Assert.Equal(DeliveryStatus.Delivered, stored.Status);
        Assert.Equal(original.WeightKg, stored.WeightKg);
        Assert.Equal(original.Recipient.Name, stored.RecipientName);
        Assert.Equal(original.Recipient.Phone, stored.RecipientPhone);
        Assert.Equal(original.Recipient.Address, stored.Address);
        Assert.Equal(original.Recipient.City, stored.City);
    }

    [Fact]
    public async Task Upload_ForAnotherCourier_Returns403AndDoesNotPersistScan()
    {
        await using var database = await SqlTestDatabase.CreateAsync();
        await using var factory = new SqlApiFactory(database.ConnectionString);
        using var client = factory.CreateAuthenticatedClient();
        await CreateShipmentAsync(client);
        var scan = Scan("scan::33333333-3333-4333-8333-333333333333", "Pickup", 8);
        scan.CourierId = "another-fictional-courier";

        using var response = await client.PostAsJsonAsync(Route + "/scans", new[] { scan });
        await AssertProblemAsync(response, HttpStatusCode.Forbidden);

        await using var verification = database.CreateContext();
        Assert.False(await verification.Set<CourierScanRecord>().AnyAsync());
        Assert.Equal(1, await verification.Set<TrackedShipment>().CountAsync());
    }

    [Fact]
    public async Task Delete_CourierThenAdmin_EnforcesRolesAndPersistsOnlyAllowedDeletion()
    {
        await using var database = await SqlTestDatabase.CreateAsync();
        await using var factory = new SqlApiFactory(database.ConnectionString);
        using var courier = factory.CreateAuthenticatedClient();
        await CreateShipmentAsync(courier);

        using var denied = await courier.DeleteAsync(Route);
        await AssertProblemAsync(denied, HttpStatusCode.Forbidden);
        await using (var verification = database.CreateContext())
            Assert.True(await verification.Set<TrackedShipment>().AnyAsync(s => s.TrackingNumber == Tracking));

        using var admin = factory.CreateAuthenticatedClient("Admin");
        using var allowed = await admin.DeleteAsync(Route);
        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
        await using var afterDelete = database.CreateContext();
        Assert.False(await afterDelete.Set<TrackedShipment>().AnyAsync());
    }

    private static async Task<ShipmentV2> CreateShipmentAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/v2/shipments", new CreateShipmentRequest
        {
            TrackingNumber = Tracking, WeightKg = 1.25m, RecipientName = "Fictional Recipient",
            RecipientPhone = "+919876543210", Address = "10 Fictional Road", City = "Pune"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        using var fetched = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        var shipment = await fetched.Content.ReadFromJsonAsync<ShipmentV2>();
        Assert.NotNull(shipment);
        Assert.Equal(Tracking, shipment.TrackingNumber);
        return shipment;
    }

    private static ScanUploadRequest Scan(string id, string type, int hour) => new()
    {
        Id = id, TrackingNumber = Tracking, ScanType = type,
        CourierId = TestAuthentication.CourierId, Location = "Fictional Pune Depot",
        ScannedAtUtc = new DateTimeOffset(2026, 1, 1, hour, 0, 0, TimeSpan.Zero),
        Notes = "Original fictional scan"
    };

    private static async Task<ScanUploadResponse> UploadAsync(HttpClient client, ScanUploadRequest[] scans)
    {
        using var response = await client.PostAsJsonAsync(Route + "/scans", scans);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ScanUploadResponse>();
        Assert.NotNull(result);
        return result;
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)status, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
    }
}
