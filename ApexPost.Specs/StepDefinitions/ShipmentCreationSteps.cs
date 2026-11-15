using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using ApexPost.Api.Contracts;
using ApexPost.Domain.Delivery;
using ApexPost.Infrastructure.Delivery;
using ApexPost.Specs.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace ApexPost.Specs.StepDefinitions;

// GitHub Copilot-assisted draft; review: docs/week5/COPILOT-BDD-REVIEW.md.
[Binding]
public sealed class ShipmentCreationSteps(ShipmentScenarioState state)
{
    // Given = Arrange; When = Act; Then = Assert.
    [Given("an authenticated courier")]
    public void GivenAnAuthenticatedCourier()
    {
        Assert.Contains("Courier", state.Client.DefaultRequestHeaders.GetValues("X-Week5-Test-Role"));
    }

    [Given("the isolated test database is empty")]
    public async Task GivenAnEmptyDatabaseAsync()
    {
        await using var db = state.Database.CreateContext();
        Assert.False(await db.Set<TrackedShipment>().AnyAsync());
        Assert.False(await db.Set<CourierScanRecord>().AnyAsync());
    }

    [Given("the courier has a valid shipment request")]
    public void GivenAValidRequest() => state.Request = ShipmentScenarioState.NewRequest();

    [Given("the courier has a valid shipment request with tracking number {string}")]
    public void GivenARequestWithTracking(string tracking) => state.Request = ShipmentScenarioState.NewRequest(tracking);

    [Given("the courier has a shipment request with weight {string} kilograms")]
    public void GivenARequestWithWeight(string weight)
    {
        state.Request = ShipmentScenarioState.NewRequest();
        state.Request.WeightKg = decimal.Parse(weight, CultureInfo.InvariantCulture);
    }

    [Given("a shipment already exists with tracking number {string}")]
    public async Task GivenAnExistingShipmentAsync(string tracking)
    {
        var existing = ShipmentScenarioState.NewRequest(tracking);
        existing.RecipientName = "Existing Fictional Recipient";
        existing.City = "Bengaluru";
        using var response = await state.Client.PostAsJsonAsync("/api/v2/shipments", existing);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        state.Request = ShipmentScenarioState.NewRequest(tracking);
        state.ExistingRequest = existing;
    }

    [When("the courier creates the shipment")]
    public async Task WhenTheCourierCreatesAsync() => state.SetResponse(
        await state.Client.PostAsJsonAsync("/api/v2/shipments", state.Request));

    [Then(@"^the response status should be (\d+) (?:Created|Conflict|Bad Request)$")]
    public async Task ThenTheResponseStatusAsync(int status)
    {
        Assert.Equal((HttpStatusCode)status, state.Response.StatusCode);
        if (status < 400)
            return;

        Assert.Equal(
            status == 400 ? "application/json" : "application/problem+json",
            state.Response.Content.Headers.ContentType?.MediaType);
        if (status == 400)
        {
            var validation = await state.Response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            Assert.NotNull(validation);
            Assert.Equal(status, validation.Status);
            Assert.True(validation.Errors.Keys.Any(key => key.EndsWith("WeightKg", StringComparison.Ordinal)),
                "The rejection must identify weight validation, not an unrelated failure.");
        }
        else
        {
            var problem = await state.Response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal(status, problem.Status);
        }
    }

    [Then("the Location header should point to a working shipment resource")]
    public async Task ThenTheLocationWorksAsync()
    {
        Assert.NotNull(state.Response.Headers.Location);
        using var response = await state.Client.GetAsync(state.Response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var shipment = await response.Content.ReadFromJsonAsync<ShipmentV2>();
        Assert.NotNull(shipment);
        Assert.Equal(state.Request.TrackingNumber, shipment.TrackingNumber);
        Assert.Equal(DeliveryStatus.Created, shipment.Status);
    }

    [Then("the persisted shipment should match the submitted request")]
    public async Task ThenTheShipmentMatchesAsync()
    {
        await using var db = state.Database.CreateContext();
        var stored = await db.Set<TrackedShipment>().AsNoTracking().SingleAsync();
        AssertDetails(state.Request, stored);
        Assert.Equal(DeliveryStatus.Created, stored.Status);
    }

    [Then("only one shipment record exists for that tracking number")]
    public async Task ThenOnlyOneShipmentAsync()
    {
        await using var db = state.Database.CreateContext();
        var rows = await db.Set<TrackedShipment>().AsNoTracking().ToArrayAsync();
        Assert.Equal(state.Request.TrackingNumber, Assert.Single(rows).TrackingNumber);
    }

    [Then("the existing shipment details are unchanged")]
    public async Task ThenExistingDetailsUnchangedAsync()
    {
        Assert.NotNull(state.ExistingRequest);
        await using var db = state.Database.CreateContext();
        AssertDetails(state.ExistingRequest, await db.Set<TrackedShipment>().AsNoTracking().SingleAsync());
    }

    [Then("no shipment is persisted")]
    public async Task ThenNoShipmentAsync()
    {
        await using var db = state.Database.CreateContext();
        Assert.False(await db.Set<TrackedShipment>().AnyAsync());
    }

    private static void AssertDetails(CreateShipmentRequest expected, TrackedShipment actual)
    {
        Assert.Equal(expected.TrackingNumber, actual.TrackingNumber);
        Assert.Equal(expected.WeightKg, actual.WeightKg);
        Assert.Equal(expected.RecipientName, actual.RecipientName);
        Assert.Equal(expected.RecipientPhone, actual.RecipientPhone);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.City, actual.City);
    }
}
