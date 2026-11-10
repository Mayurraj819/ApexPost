using System.Net;
using System.Text;
using System.Text.Json;
using ApexPost.Courier.Integration;
using ApexPost.Courier.Offline.Abstractions;
using ApexPost.Courier.Offline.Models;
using Microsoft.Extensions.DependencyInjection;
using Polly.CircuitBreaker;

namespace ApexPost.Courier.Integration.Tests;
public sealed class TestTokens : ITokenStore
{
    public string? Token = "test-only-not-a-real-token";
    public int Clears;
    public event Action? Cleared;
    public Task<string?> GetAsync() => Task.FromResult(Token);
    public Task SetAsync(string token)
    {
        Token = token;
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Clears++;
        Token = null;
        Cleared?.Invoke();
        return Task.CompletedTask;
    }
}

public sealed class MemoryScans : IScanRepository
{
    public readonly Dictionary<string, ShipmentScan> Rows = [];
    public ShipmentScan Create(ShipmentScan s)
    {
        Rows.Add(s.Id, s with { SyncStatus = SyncStatus.Pending });
        return Rows[s.Id];
    }

    public ShipmentScan? GetById(string id) => Rows.GetValueOrDefault(id);
    public void Update(ShipmentScan s) => Rows[s.Id] = s;
    public bool Delete(string id) => Rows.Remove(id);
    public IReadOnlyList<ShipmentScan> ListAll() => Rows.Values.ToArray();
    public IReadOnlyList<ShipmentScan> FindByTrackingNumber(string t) => Rows.Values.Where(s => s.TrackingNumber == t).ToArray();
    public IReadOnlyList<ShipmentScan> ListPending() => Rows.Values.Where(s => s.SyncStatus == SyncStatus.Pending).ToArray();
    public IReadOnlyDictionary<ScanType, long> CountByScanType(string c) => Rows.Values.Where(s => s.CourierId == c).GroupBy(s => s.ScanType).ToDictionary(g => g.Key, g => (long)g.Count());
}

public sealed class Stub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    public int Calls;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
    {
        Calls++;
        var result = await send(r, ct);
        result.RequestMessage = r;
        return result;
    }
}

public sealed class CourierTests
{
    private static ServiceProvider Provider(Stub handler, TestTokens? tokens = null, bool breaker = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITokenStore>(tokens ?? new TestTokens());
        services.AddCourierApi(new Uri("https://localhost/"), o =>
        {
            o.Retry.Delay = TimeSpan.Zero;
            o.Retry.UseJitter = false;
            o.CircuitBreaker.MinimumThroughput = breaker ? 3 : 100;
            o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(1);
        }).ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider();
    }

    private static HttpResponseMessage Response(int status, string body = "{}") => new((HttpStatusCode)status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };
    private static ShipmentScan Scan(string courier = "COURIER-01") => new()
    {
        TrackingNumber = "APX-12345678",
        ScanType = ScanType.Pickup,
        CourierId = courier,
        Location = "Demo",
        ScannedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
    };
    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(429)]
    public async Task Nontransient_errors_are_not_retried(int status)
    {
        var tokens = new TestTokens();
        var stub = new Stub((_, _) => Task.FromResult(Response(status, "{\"title\":\"Rejected\"}")));
        await using var p = Provider(stub, tokens);
        await Assert.ThrowsAsync<CourierApiException>(() => p.GetRequiredService<ApexApiClient>().ValidateLoginAsync());
        Assert.Equal(1, stub.Calls);
        Assert.Equal(status == 401 ? 1 : 0, tokens.Clears);
    }

    [Theory]
    [InlineData(408)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task Transient_errors_get_three_attempts(int status)
    {
        var stub = new Stub((_, _) => Task.FromResult(Response(status)));
        await using var p = Provider(stub);
        await Assert.ThrowsAsync<CourierApiException>(() => p.GetRequiredService<ApexApiClient>().ValidateLoginAsync());
        Assert.Equal(3, stub.Calls);
    }

    [Fact]
    public async Task Unsafe_post_is_not_retried()
    {
        var stub = new Stub((_, _) => Task.FromResult(Response(503)));
        await using var p = Provider(stub);
        using var http = p.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(ApexApiClient));
        using var result = await http.PostAsync("api/v2/shipments", new StringContent("{}"));
        Assert.Equal(1, stub.Calls);
    }

    [Fact]
    public async Task Network_errors_retry_and_bearer_attaches()
    {
        var count = 0;
        var stub = new Stub((r, _) =>
        {
            Assert.Equal("Bearer", r.Headers.Authorization?.Scheme);
            if (++count < 3)
                throw new HttpRequestException("Injected fault");
            return Task.FromResult(Response(200, "{\"items\":[],\"unknown\":123}"));
        });
        await using var p = Provider(stub);
        await p.GetRequiredService<ApexApiClient>().ValidateLoginAsync();
        Assert.Equal(3, stub.Calls);
    }

    [Fact]
    public async Task Batches_of_twenty_only_sync_current_owner()
    {
        var sizes = new List<int>();
        var stub = new Stub(async (r, ct) =>
        {
            using var json = JsonDocument.Parse(await r.Content!.ReadAsStringAsync(ct));
            var rows = json.RootElement.EnumerateArray().ToArray();
            sizes.Add(rows.Length);
            Assert.All(rows, x => Assert.Equal("Pickup", x.GetProperty("scanType").GetString()));
            return Response(200, JsonSerializer.Serialize(new { accepted = rows.Length, duplicatesSkipped = 0, rejected = 0, acceptedScanIds = rows.Select(x => x.GetProperty("id").GetString()), duplicateScanIds = Array.Empty<string>(), rejections = Array.Empty<object>() }));
        });
        await using var p = Provider(stub);
        var repo = new MemoryScans();
        for (var i = 0; i < 45; i++)
            repo.Create(Scan());
        repo.Create(Scan("OTHER"));
        using var sync = new CourierSync(repo, p.GetRequiredService<ApexApiClient>(), p.GetRequiredService<CourierState>());
        await sync.SyncAsync("COURIER-01");
        Assert.Equal(new[] { 20, 20, 5 }, sizes);
        Assert.Equal(45, repo.Rows.Values.Count(s => s.SyncStatus == SyncStatus.Synced));
        Assert.Single(repo.ListPending());
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{bad json")]
    [InlineData("{\"accepted\":1}")]
    public async Task Invalid_results_keep_pending(string json)
    {
        var stub = new Stub((_, _) => Task.FromResult(Response(200, json)));
        await using var p = Provider(stub);
        var repo = new MemoryScans();
        repo.Create(Scan());
        using var sync = new CourierSync(repo, p.GetRequiredService<ApexApiClient>(), p.GetRequiredService<CourierState>());
        await sync.SyncAsync("COURIER-01");
        Assert.Single(repo.ListPending());
        Assert.True(p.GetRequiredService<CourierState>().ServerUnavailable);
    }

    [Fact]
    public async Task Expired_login_clears_token_keeps_pending()
    {
        var tokens = new TestTokens();
        var signOut = false;
        tokens.Cleared += () => signOut = true;
        var stub = new Stub((_, _) => Task.FromResult(Response(401)));
        await using var p = Provider(stub, tokens);
        var repo = new MemoryScans();
        repo.Create(Scan());
        using var sync = new CourierSync(repo, p.GetRequiredService<ApexApiClient>(), p.GetRequiredService<CourierState>());
        await sync.SyncAsync("COURIER-01");
        Assert.Null(tokens.Token);
        Assert.True(signOut);
        Assert.Single(repo.ListPending());
    }

    [Fact]
    public async Task Circuit_opens_then_recovers()
    {
        var available = false;
        var stub = new Stub((_, _) => Task.FromResult(available ? Response(200, "{\"items\":[]}") : Response(503)));
        await using var p = Provider(stub, breaker: true);
        var api = p.GetRequiredService<ApexApiClient>();
        await Assert.ThrowsAnyAsync<Exception>(() => api.ValidateLoginAsync());
        Assert.Equal("Open", p.GetRequiredService<CourierState>().Circuit);
        var calls = stub.Calls;
        await Assert.ThrowsAsync<BrokenCircuitException>(() => api.ValidateLoginAsync());
        Assert.Equal(calls, stub.Calls);
        available = true;
        await Task.Delay(1200);
        await api.ValidateLoginAsync();
        Assert.Equal("Closed", p.GetRequiredService<CourierState>().Circuit);
    }

    [Fact]
    public async Task Duplicate_ack_synced_and_rejection_failed()
    {
        var repo = new MemoryScans();
        var d = repo.Create(Scan());
        var r = repo.Create(Scan());
        var stub = new Stub((_, _) => Task.FromResult(Response(200, JsonSerializer.Serialize(new { accepted = 0, duplicatesSkipped = 1, rejected = 1, acceptedScanIds = Array.Empty<string>(), duplicateScanIds = new[] { d.Id }, rejections = new[] { new { id = r.Id, reason = "Invalid transition" } } }))));
        await using var p = Provider(stub);
        using var sync = new CourierSync(repo, p.GetRequiredService<ApexApiClient>(), p.GetRequiredService<CourierState>());
        await sync.SyncAsync("COURIER-01");
        Assert.Equal(SyncStatus.Synced, repo.GetById(d.Id)!.SyncStatus);
        Assert.Equal(SyncStatus.Failed, repo.GetById(r.Id)!.SyncStatus);
    }

    [Fact]
    public async Task Attempt_timeouts_retry_but_caller_cancellation_does_not()
    {
        var stub = new Stub(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromMinutes(1), ct);
            return Response(200);
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITokenStore>(new TestTokens());
        services.AddCourierApi(new Uri("https://localhost/"), o =>
        {
            o.Retry.Delay = TimeSpan.Zero;
            o.Retry.UseJitter = false;
            o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);
            o.CircuitBreaker.MinimumThroughput = 100;
        }).ConfigurePrimaryHttpMessageHandler(() => stub);
        await using var p = services.BuildServiceProvider();
        var api = p.GetRequiredService<ApexApiClient>();
        await Assert.ThrowsAsync<Polly.Timeout.TimeoutRejectedException>(() => api.ValidateLoginAsync());
        Assert.Equal(3, stub.Calls);
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(40));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => api.ValidateLoginAsync(cancel.Token));
        Assert.Equal(4, stub.Calls);
    }
}
