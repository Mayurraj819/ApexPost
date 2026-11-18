using System.Net;
using System.Net.Http.Json;
using ApexPost.Api.Contracts;
using ApexPost.Domain.Delivery;
using ApexPost.Infrastructure.Delivery;
using ApexPost.IntegrationTests.Infrastructure;
using ApexPost.Specs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace ApexPost.Specs.StepDefinitions;

// Added during the ChatGPT review stage, not attributed to GitHub Copilot.
[Binding]
public sealed class ScanUploadSteps(ShipmentScenarioState state)
{
    private const string OriginalNotes = "Original fictional scan";
    private string ScanRoute => $"/api/v2/shipments/{state.Request.TrackingNumber}/scans";

    [Given("the courier has two valid scans for the shipment")]
    public void GivenTwoScans() => state.Scans =
    [
        NewScan("scan::11111111-1111-4111-8111-111111111111", "Pickup", 8),
        NewScan("scan::22222222-2222-4222-8222-222222222222", "InTransit", 9)
    ];

    [Given("those scans were already accepted")]
    public async Task GivenAlreadyAcceptedAsync()
    {
        using var response = await state.Client.PostAsJsonAsync(ScanRoute, state.Scans);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ScanUploadResponse>();
        Assert.NotNull(result);
        Assert.Equal((2, 0, 0), (result.Accepted, result.DuplicatesSkipped, result.Rejected));
    }

    [Given("the courier changes the first scan notes without changing its identifier")]
    public void GivenChangedNotes() => state.Scans[0].Notes = "Changed notes must not overwrite history";

    [When("the courier uploads the scans")]
    public async Task WhenUploadAsync() => state.SetResponse(await state.Client.PostAsJsonAsync(ScanRoute, state.Scans));

    [When("the courier uploads only the changed scan")]
    public async Task WhenUploadChangedAsync() => state.SetResponse(await state.Client.PostAsJsonAsync(ScanRoute, new[] { state.Scans[0] }));

    [Then(@"^(\d+) scans are accepted, (\d+) duplicates are skipped and (\d+) scans are rejected$")]
    public async Task ThenUploadCountsAsync(int accepted, int duplicates, int rejected)
    {
        Assert.Equal(HttpStatusCode.OK, state.Response.StatusCode);
        var result = await state.Response.Content.ReadFromJsonAsync<ScanUploadResponse>();
        Assert.NotNull(result);
        state.UploadResult = result;
        Assert.Equal((accepted, duplicates, rejected), (result.Accepted, result.DuplicatesSkipped, result.Rejected));
        Assert.Equal(accepted, result.AcceptedScanIds.Count);
        Assert.Equal(duplicates, result.DuplicateScanIds.Count);
        Assert.Equal(rejected, result.Rejections.Count);
    }

    [Then("the changed scan identifier is reported as rejected")]
    public void ThenChangedIdRejected()
    {
        var result = state.UploadResult;
        Assert.NotNull(result);
        Assert.Equal(state.Scans[0].Id, Assert.Single(result.Rejections).Id);
    }

    [Then("the shipment history contains the two original scans exactly once")]
    public async Task ThenOriginalHistoryAsync()
    {
        await using var db = state.Database.CreateContext();
        var shipment = await db.Set<TrackedShipment>().AsNoTracking().SingleAsync();
        var stored = await db.Set<CourierScanRecord>().AsNoTracking().OrderBy(s => s.ScannedAtUtc).ToArrayAsync();
        Assert.Equal(2, stored.Length);
        for (var i = 0; i < stored.Length; i++)
        {
            Assert.Equal(state.Scans[i].Id, stored[i].Id);
            Assert.Equal(shipment.Id, stored[i].ShipmentId);
            Assert.Equal(Enum.Parse<CourierScanType>(state.Scans[i].ScanType), stored[i].ScanType);
            Assert.Equal(TestAuthentication.CourierId, stored[i].CourierId);
            Assert.Equal(state.Scans[i].Location, stored[i].Location);
            Assert.Equal(state.Scans[i].ScannedAtUtc, stored[i].ScannedAtUtc);
            Assert.Equal(OriginalNotes, stored[i].Notes);
        }
    }

    private ScanUploadRequest NewScan(string id, string type, int hour) => new()
    {
        Id = id, TrackingNumber = state.Request.TrackingNumber, ScanType = type,
        CourierId = TestAuthentication.CourierId, Location = "Fictional Pune Depot",
        ScannedAtUtc = new DateTimeOffset(2026, 1, 1, hour, 0, 0, TimeSpan.Zero), Notes = OriginalNotes
    };
}
