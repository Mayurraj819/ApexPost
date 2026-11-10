using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ApexPost.Courier.Offline.Abstractions;
using ApexPost.Courier.Offline.Models;

namespace ApexPost.Courier.Integration;
public sealed class AuthHeaderHandler(ITokenStore tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await tokens.GetAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await base.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            await tokens.ClearAsync();
        return response;
    }
}

public sealed class ApexApiClient(HttpClient http) : IShipmentUploader
{
    public static readonly HttpRequestOptionsKey<bool> IdempotentScanUpload = new("ApexPost.IdempotentScanUpload");
    public async Task ValidateLoginAsync(CancellationToken ct = default)
    {
        using var overall = CancellationTokenSource.CreateLinkedTokenSource(ct);
        overall.CancelAfter(TimeSpan.FromSeconds(30));
        ct = overall.Token;
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/shipments?pageSize=1");
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        await EnsureSuccess(response, ct);
        _ = await Read(response, CourierJsonContext.Default.ShipmentPage, ct);
    }

    public async Task<bool> UploadAsync(ShipmentScan scan, CancellationToken cancellationToken = default)
    {
        var result = await UploadBatchAsync(scan.TrackingNumber, [scan], cancellationToken);
        return result.AcceptedScanIds!.Contains(scan.Id) || result.DuplicateScanIds!.Contains(scan.Id);
    }

    public async Task<UploadResponse> UploadBatchAsync(string tracking, IReadOnlyList<ShipmentScan> scans, CancellationToken ct = default)
    {
        if (scans.Count is < 1 or > 20)
            throw new ArgumentException("Mobile upload batches contain 1 to 20 scans.");
        using var overall = CancellationTokenSource.CreateLinkedTokenSource(ct);
        overall.CancelAfter(TimeSpan.FromSeconds(30));
        ct = overall.Token;
        var body = scans.Select(s => new UploadScan(s.Id, s.TrackingNumber, s.ScanType, s.CourierId, s.Location, s.ScannedAtUtc, s.Notes)).ToList();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v2/shipments/{Uri.EscapeDataString(tracking)}/scans");
        request.Options.Set(IdempotentScanUpload, true);
        request.Content = JsonContent.Create(body, CourierJsonContext.Default.ListUploadScan);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        await EnsureSuccess(response, ct);
        var result = await Read(response, CourierJsonContext.Default.UploadResponse, ct);
        var accepted = result.AcceptedScanIds;
        var duplicates = result.DuplicateScanIds;
        var rejected = result.Rejections;
        if (accepted is null || duplicates is null || rejected is null || rejected.Any(r => r is null) || result.Accepted != accepted.Count || result.DuplicatesSkipped != duplicates.Count || result.Rejected != rejected.Count)
            throw new CourierProtocolException("The server returned an incomplete sync result; scans remain pending.");
        var expected = scans.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        var reported = accepted.Concat(duplicates).Concat(rejected.Select(r => r.Id ?? "")).ToArray();
        if (reported.Length != expected.Count || reported.Distinct(StringComparer.Ordinal).Count() != reported.Length || reported.Any(id => !expected.Contains(id)))
            throw new CourierProtocolException("The sync response did not identify every scan exactly once.");
        return result;
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;
        var message = $"Server returned HTTP {(int)response.StatusCode}.";
        try
        {
            var problem = await Read(response, CourierJsonContext.Default.ApiProblem, ct);
            var errors = problem.Errors?.Values.SelectMany(x => x ?? []).Take(5);
            message = string.Join(" ", new[] { problem.Title, problem.Detail }.Concat(errors ?? []).Where(x => !string.IsNullOrWhiteSpace(x)));
            if (string.IsNullOrWhiteSpace(message))
                message = "The server rejected the request.";
            if (message.Length > 1000)
                message = message[..1000];
        }
        catch (CourierProtocolException)
        {
        }

        throw new CourierApiException((int)response.StatusCode, message);
    }

    private static async Task<T> Read<T>(HttpResponseMessage response, JsonTypeInfo<T> type, CancellationToken ct)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var data = new MemoryStream();
            var buffer = new byte[4096];
            int read;
            while ((read = await stream.ReadAsync(buffer, ct)) > 0)
            {
                if (data.Length + read > 65536)
                    throw new CourierProtocolException("The server response exceeded the expected size.");
                await data.WriteAsync(buffer.AsMemory(0, read), ct);
            }

            return JsonSerializer.Deserialize(data.ToArray(), type) ?? throw new CourierProtocolException("The server returned an empty response.");
        }
        catch (JsonException)
        {
            throw new CourierProtocolException("The server returned malformed JSON. Your scans remain stored locally.");
        }
    }
}
