using System.Text.Json.Serialization;
using ApexPost.Courier.Offline.Models;

namespace ApexPost.Courier.Integration;
public sealed record UploadScan(string Id, string TrackingNumber, ScanType ScanType, string CourierId, string Location, DateTimeOffset ScannedAtUtc, string Notes);
public sealed record RejectedScan(string? Id, string? Reason);
public sealed record UploadResponse(int Accepted, int DuplicatesSkipped, int Rejected, List<string>? AcceptedScanIds, List<string>? DuplicateScanIds, List<RejectedScan>? Rejections);
public sealed record Recipient(string? Name, string? Phone, string? Address, string? City);
public sealed record LatestScan(string? ScanType, string? Location, DateTimeOffset ScannedAtUtc);
public sealed record Shipment(string? TrackingNumber, decimal WeightKg, Recipient? Recipient, string? Status, LatestScan? LatestScan);
public sealed record ShipmentPage(List<Shipment>? Items, int Page, int PageSize, int TotalCount);
public sealed record ApiProblem(string? Title, string? Detail, int? Status, Dictionary<string, string[]>? Errors);
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, UseStringEnumConverter = true, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(List<UploadScan>))]
[JsonSerializable(typeof(UploadResponse))]
[JsonSerializable(typeof(ShipmentPage))]
[JsonSerializable(typeof(ApiProblem))]
public partial class CourierJsonContext : JsonSerializerContext;
public interface ITokenStore
{
    Task<string?> GetAsync();
    Task SetAsync(string token);
    Task ClearAsync();
    event Action? Cleared;
}

public sealed class CourierApiException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

public sealed class CourierProtocolException(string message) : Exception(message);
public sealed class CourierState
{
    public event Action? Changed;
    public string Circuit { get; private set; } = "Closed";
    public string Message { get; private set; } = "Ready";
    public DateTimeOffset? LastSyncUtc { get; private set; }
    public int Failures { get; private set; }
    public bool ServerUnavailable { get; private set; }

    public void SetCircuit(string value)
    {
        Circuit = value;
        ServerUnavailable = value != "Closed";
        Changed?.Invoke();
    }

    public void Report(string message, bool unavailable = false)
    {
        Message = message;
        ServerUnavailable = unavailable || Circuit != "Closed";
        Changed?.Invoke();
    }

    public void Completed(int failures)
    {
        LastSyncUtc = DateTimeOffset.UtcNow;
        Failures = failures;
        Report($"Sync finished. Failed scans: {failures}.");
    }
}
