using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ApexPost.Application.Delivery;
using ApexPost.Domain.Delivery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApexPost.Api.Tests;
public sealed class ApiFactory(string environment = "Development", bool explode = false, bool authenticate = true) : WebApplicationFactory<ApiProgram>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("Jwt:Issuer",SecurityTokens.Issuer);
        builder.UseSetting("Jwt:Audience",SecurityTokens.Audience);
        builder.UseSetting("Jwt:SigningKey",Convert.ToBase64String(SecurityTokens.Key));
        builder.UseSetting("Partners:ApiKeys:0",SecurityTokens.PartnerKey);
        builder.UseSetting("ConnectionStrings:ApexPost", "Server=localhost;Database=Api_Transport_NoConnection;Integrated Security=true;TrustServerCertificate=true");
        builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:ApexPost"] = "Server=localhost;Database=Api_Transport_NoConnection;Integrated Security=true;TrustServerCertificate=true" }));
        builder.ConfigureServices(s =>
        {
            if(authenticate) s.AddAuthentication(o=>{o.DefaultAuthenticateScheme="TransportTest";o.DefaultChallengeScheme="TransportTest";o.DefaultForbidScheme="TransportTest";}).AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,TransportAuthentication>("TransportTest",_=>{});
            s.RemoveAll<IShipmentApiService>();
            s.AddSingleton<IShipmentApiService>(new MemoryService(explode));
        });
    }
}

// Test double is ONLY for HTTP binding/serialization/error/metadata tests, never production persistence.
internal sealed class MemoryService(bool explode) : IShipmentApiService
{
    private readonly Dictionary<string, TrackedShipment> _rows = [];
    private readonly Dictionary<string, ScanInput> _scans = [];
    private TrackedShipment Get(string t)
    {
        if (explode)
            throw new Exception("INTERNAL-SECRET-DETAIL");
        return _rows.GetValueOrDefault(t) ?? throw new ShipmentNotFoundException(t);
    }

    private ShipmentView View(TrackedShipment s)
    {
        var l = _scans.Values.Where(x => x.TrackingNumber == s.TrackingNumber).OrderByDescending(x => x.ScannedAtUtc).FirstOrDefault();
        return new(s.TrackingNumber, s.WeightKg, s.RecipientName, s.RecipientPhone, s.Address, s.City, s.Status, l is null ? null : new(l.Id, l.TrackingNumber, l.ScanType, l.CourierId, l.Location, l.ScannedAtUtc, l.Notes));
    }

    public Task<ShipmentView> GetAsync(string t, CancellationToken ct) => Task.FromResult(View(Get(t)));
    public Task<ShipmentPage> ListAsync(int page, int size, DeliveryStatus? status, string? city, CancellationToken ct)
    {
        var q = _rows.Values.Where(s => (status == null || s.Status == status) && (city == null || s.City == city)).ToArray();
        return Task.FromResult(new ShipmentPage(q.Skip((page - 1) * size).Take(size).Select(View).ToArray(), page, size, q.Length));
    }

    public Task<ShipmentView> CreateAsync(string t, ShipmentDetails d, CancellationToken ct)
    {
        if (_rows.ContainsKey(t))
            throw new DeliveryConflictException("Duplicate tracking number.");
        var s = new TrackedShipment(t, d.WeightKg, d.RecipientName, d.RecipientPhone, d.Address, d.City);
        _rows.Add(t, s);
        return Task.FromResult(View(s));
    }

    public Task<ShipmentView> ReplaceAsync(string t, ShipmentDetails d, CancellationToken ct)
    {
        var s = Get(t);
        s.ReplaceDetails(d.WeightKg, d.RecipientName, d.RecipientPhone, d.Address, d.City);
        return Task.FromResult(View(s));
    }

    public Task<ShipmentView> ChangeStatusAsync(string t, DeliveryStatus status, CancellationToken ct)
    {
        var s = Get(t);
        s.ChangeStatus(status);
        return Task.FromResult(View(s));
    }

    public Task DeleteAsync(string t, CancellationToken ct)
    {
        Get(t).EnsureCanDelete();
        _rows.Remove(t);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ScanView>> ScansAsync(string t, CancellationToken ct)
    {
        Get(t);
        return Task.FromResult<IReadOnlyList<ScanView>>(_scans.Values.Where(s => s.TrackingNumber == t).Select(s => new ScanView(s.Id, t, s.ScanType, s.CourierId, s.Location, s.ScannedAtUtc, s.Notes)).ToArray());
    }

    public Task<ScanUploadResult> UploadAsync(string t, IReadOnlyList<ScanInput> scans, CancellationToken ct)
    {
        Get(t);
        var a = new List<string>();
        var d = new List<string>();
        var r = new List<ScanRejection>();
        foreach (var s in scans)
        {
            if (s.TrackingNumber != t)
                r.Add(new(s.Id, "Route mismatch"));
            else if (_scans.ContainsKey(s.Id))
                d.Add(s.Id);
            else
            {
                _scans.Add(s.Id, s);
                a.Add(s.Id);
            }
        }

        return Task.FromResult(new ScanUploadResult(a.Count, d.Count, r.Count, a, d, r));
    }
}

public sealed class TransportTests
{
    internal static object Create(string tracking = "APX-12345678") => new
    {
        trackingNumber = tracking,
        weightKg = 1.25m,
        recipientName = "Fictional recipient",
        recipientPhone = "+919876543210",
        address = "Demo street",
        city = "Pune"
    };
    [Theory]
    [InlineData("/api/shipments")]
    [InlineData("/api/v1/shipments")]
    [InlineData("/api/v2/shipments")]
    public async Task Crud_status_and_location_are_correct(string root)
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var created = await client.PostAsJsonAsync(root, Create());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(created.Headers.Location)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(root, Create())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(root + "/APX-12345678", Create())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PatchAsJsonAsync(root + "/APX-12345678/status", new { status = "Delivered" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync(root + "/APX-12345678/status", new { status = "PickedUp" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync(root + "/APX-12345678")).StatusCode);
        await client.PostAsJsonAsync(root, Create("APX-87654321"));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(root + "/APX-87654321")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(root + "/APX-87654321")).StatusCode);
    }

    [Theory]
    [InlineData("v1")]
    [InlineData("v2")]
    public async Task Validation_wrong_media_and_scan_contracts(string version)
    {
        await using var f = new ApiFactory();
        using var c = f.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var root = $"/api/{version}/shipments";
        var invalid = await c.PostAsJsonAsync(root, new { });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains("errors", await invalid.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await c.PostAsync(root, new StringContent("text", Encoding.UTF8, "text/plain"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync(root + "?pageSize=51")).StatusCode);
        await c.PostAsJsonAsync(root, Create());
        var scan = new
        {
            id = "scan::" + Guid.NewGuid(),
            trackingNumber = "APX-12345678",
            scanType = "Pickup",
            courierId = "COURIER-01",
            location = "Demo",
            scannedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            notes = ""
        };
        var first = await c.PostAsJsonAsync(root + "/APX-12345678/scans", new[] { scan });
        Assert.Equal(1, (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accepted").GetInt32());
        var again = await c.PostAsJsonAsync(root + "/APX-12345678/scans", new[] { scan });
        Assert.Equal(1, (await again.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duplicatesSkipped").GetInt32());
        var bad = await c.PostAsJsonAsync(root + "/APX-12345678/scans", new[] { new { scan.id, scan.trackingNumber, scan.scanType, scan.courierId, scan.location, scannedAtUtc = DateTimeOffset.UtcNow.AddDays(1) } });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var detail = await c.GetFromJsonAsync<JsonElement>(root + "/APX-12345678");
        if (version == "v2")
        {
            Assert.Equal("Created", detail.GetProperty("status").GetString());
            Assert.True(detail.TryGetProperty("recipient", out _));
            Assert.True(detail.TryGetProperty("latestScan", out _));
        }
        else
        {
            Assert.Equal(0, detail.GetProperty("status").GetInt32());
            Assert.True(detail.TryGetProperty("recipientName", out _));
        }

        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync(root + "/APX-12345678/scans")).StatusCode);
    }

    [Fact]
    public async Task Production_errors_hide_details_and_documents()
    {
        await using var f = new ApiFactory("Production", true);
        using var c = f.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var error = await c.GetAsync("/api/v2/shipments/APX-12345678");
        Assert.Equal(HttpStatusCode.InternalServerError, error.StatusCode);
        Assert.DoesNotContain("INTERNAL-SECRET", await error.Content.ReadAsStringAsync());
        Assert.Equal("application/problem+json", error.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/openapi/v2.json")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/swagger/index.html")).StatusCode);
    }

    [Fact]
    public async Task Openapi_has_separate_documents_stable_names_and_deprecation_headers()
    {
        await using var f = new ApiFactory();
        using var c = f.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        foreach (var v in new[]
        {
            "v1",
            "v2"
        }

        )
        {
            var document = await c.GetFromJsonAsync<JsonElement>($"/openapi/{v}.json");
            Assert.Equal("ApexPost Logistics API", document.GetProperty("info").GetProperty("title").GetString());
            var contractPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../contracts", v + ".json"));
            Assert.True(JsonElement.DeepEquals(JsonDocument.Parse(await File.ReadAllTextAsync(contractPath)).RootElement, document), "Committed contract differs from runtime OpenAPI; regenerate it.");
            var paths = document.GetProperty("paths");
            Assert.Equal(v=="v1"?4:5, paths.EnumerateObject().Count());
            if (v == "v1")
                Assert.True(paths.GetProperty("/api/v1/shipments/{trackingNumber}").GetProperty("get").GetProperty("deprecated").GetBoolean());
            Assert.Equal("GetShipment" + v.ToUpperInvariant(), paths.GetProperty($"/api/{v}/shipments/{{trackingNumber}}").GetProperty("get").GetProperty("operationId").GetString());
        }

        var result = await c.GetAsync("/api/v1/shipments");
        Assert.True(result.Headers.Contains("api-deprecated-versions"));
        Assert.True(result.Headers.Contains("api-supported-versions"));
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/swagger/index.html")).StatusCode);
    }
}
