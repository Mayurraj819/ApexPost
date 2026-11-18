using System.Net;
using System.Net.Http.Json;
using ApexPost.Courier.Integration;
using ApexPost.Courier.Offline.Models;

namespace ApexPost.UnitTests;

// These are direct typed-client unit tests, not the registered Polly pipeline tests.
public sealed class ApexApiClientContractTests
{
    [Fact]
    public async Task UploadBatchAsync_ValidAcknowledgement_PreservesWireFieldsAndMapsAcceptedId()
    {
        // Arrange
        var scan = new ShipmentScan
        {
            Id = "scan::11111111-1111-4111-8111-111111111111",
            TrackingNumber = "APX-12345678", ScanType = ScanType.InTransit,
            CourierId = "fictional-courier-01", Location = "Fictional Pune Depot",
            ScannedAtUtc = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
            Notes = "Fictional contract test", SyncStatus = SyncStatus.Pending
        };
        List<UploadScan>? sent = null;
        using var handler = new FakeHandler(async (request, token) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/v2/shipments/APX-12345678/scans", request.RequestUri?.AbsolutePath);
            Assert.True(request.Options.TryGetValue(ApexApiClient.IdempotentScanUpload, out var idempotent));
            Assert.True(idempotent);
            Assert.NotNull(request.Content);
            sent = await request.Content.ReadFromJsonAsync(CourierJsonContext.Default.ListUploadScan, token);
            var body = new UploadResponse(1, 0, 0, [scan.Id], [], []);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(body, CourierJsonContext.Default.UploadResponse)
            };
        });
        using var http = NewHttpClient(handler);
        var client = new ApexApiClient(http);

        // Act
        var result = await client.UploadBatchAsync(scan.TrackingNumber, [scan]);

        // Assert
        Assert.Equal(1, handler.Calls);
        Assert.NotNull(sent);
        Assert.Equal(new UploadScan(scan.Id, scan.TrackingNumber, scan.ScanType, scan.CourierId,
            scan.Location, scan.ScannedAtUtc, scan.Notes), Assert.Single(sent));
        Assert.Equal((1, 0, 0), (result.Accepted, result.DuplicatesSkipped, result.Rejected));
        Assert.NotNull(result.AcceptedScanIds);
        Assert.Equal(scan.Id, Assert.Single(result.AcceptedScanIds));
        Assert.Empty(result.DuplicateScanIds!);
        Assert.Empty(result.Rejections!);
    }

    [Theory]
    [InlineData(404)]
    [InlineData(500)]
    public async Task ValidateLoginAsync_ProblemDetails_PreservesStatusTitleDetailAndFieldError(int status)
    {
        // Arrange: fake wire response, no DNS/server/SQL connection or retries.
        var body = new ApiProblem("Fictional failure", "Fictional explanation", status,
            new Dictionary<string, string[]> { ["trackingNumber"] = ["Fictional field error"] });
        using var handler = new FakeHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/v2/shipments", request.RequestUri?.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
            {
                Content = JsonContent.Create(body, CourierJsonContext.Default.ApiProblem,
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json"))
            });
        });
        using var http = NewHttpClient(handler);
        var client = new ApexApiClient(http);

        // Act
        var error = await Assert.ThrowsAsync<CourierApiException>(() => client.ValidateLoginAsync());

        // Assert
        Assert.Equal(status, error.Status);
        Assert.Contains("Fictional failure", error.Message);
        Assert.Contains("Fictional explanation", error.Message);
        Assert.Contains("Fictional field error", error.Message);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task ValidateLoginAsync_CallerCancelsDuringSend_PropagatesCancellationWithoutProtocolError()
    {
        // Arrange: cancellation is triggered deterministically inside the fake send.
        using var cancellation = new CancellationTokenSource();
        using var handler = new FakeHandler((_, token) =>
        {
            cancellation.Cancel();
            return Task.FromCanceled<HttpResponseMessage>(token);
        });
        using var http = NewHttpClient(handler);
        var client = new ApexApiClient(http);

        // Act
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.ValidateLoginAsync(cancellation.Token));

        // Assert: the client uses a linked token, so token identity need not match the caller's.
        Assert.True(cancellation.IsCancellationRequested);
        Assert.True(error.CancellationToken.IsCancellationRequested);
        Assert.Equal(1, handler.Calls);
    }

    private static HttpClient NewHttpClient(HttpMessageHandler handler) => new(handler, disposeHandler: false)
    {
        BaseAddress = new Uri("https://apexpost.invalid/")
    };

    private sealed class FakeHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return send(request, cancellationToken);
        }
    }
}
