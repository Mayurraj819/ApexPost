using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ApexPost.Application.Delivery;
using ApexPost.Domain.Delivery;

namespace ApexPost.Api.Contracts;
/// <summary>Contract for UpdateShipmentRequest.</summary>
public class UpdateShipmentRequest
{
    /// <summary>Shipment weight in kilograms, inclusive range 0.1 to 70.</summary>
    [Range(typeof(decimal), "0.1", "70")]
    public decimal WeightKg { get; set; }

    /// <summary>Fictional recipient full name, required.</summary>
    [Required, MaxLength(120)]
    public string RecipientName { get; set; } = "";

    /// <summary>Recipient phone: optional plus followed by 7 to 15 digits; first digit is nonzero.</summary>
    [Required, MaxLength(25), RegularExpression(@"\+?[1-9][0-9]{6,14}")]
    public string RecipientPhone { get; set; } = "";

    /// <summary>Recipient street address, required.</summary>
    [Required, MaxLength(300)]
    public string Address { get; set; } = "";

    /// <summary>Recipient city, required; list filtering uses exact database comparison.</summary>
    [Required, MaxLength(120)]
    public string City { get; set; } = "";

    /// <summary>Shipment API contract or supporting operation.</summary>
    public ShipmentDetails ToDetails() => new(WeightKg, RecipientName, RecipientPhone, Address, City);
}

/// <summary>Contract for CreateShipmentRequest.</summary>
public sealed class CreateShipmentRequest : UpdateShipmentRequest
{
    /// <summary>Tracking identifier in APX-12345678 format.</summary>
    [Required, RegularExpression(@"APX-[0-9]{8}")]
    public string TrackingNumber { get; set; } = "";
}

/// <summary>Contract for UpdateStatusRequest.</summary>
public sealed class UpdateStatusRequest
{
    /// <summary>Shipment status or optional status filter.</summary>
    [Required, RegularExpression("^(Created|PickedUp|InTransit|OutForDelivery|Delivered|Failed)$")]
    public string Status { get; set; } = "";
}

/// <summary>Contract for ShipmentFilter.</summary>
public sealed class ShipmentFilter : IValidatableObject
{
    /// <summary>One-based page number, default 1.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>Page size, default 20 and maximum 50.</summary>
    [Range(1, 50)]
    public int PageSize { get; set; } = 20;

    /// <summary>Shipment status or optional status filter.</summary>
    [EnumDataType(typeof(DeliveryStatus))]
    public DeliveryStatus? Status { get; set; }

    /// <summary>Recipient city, required; list filtering uses exact database comparison.</summary>
    [MaxLength(120)]
    public string? City { get; set; }

    /// <summary>Shipment API contract or supporting operation.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if ((long)(Page - 1) * PageSize > int.MaxValue)
            yield return new("Page offset is too large.", [nameof(Page)]);
    }
}

/// <summary>Contract for ScanUploadRequest.</summary>
public sealed class ScanUploadRequest : IValidatableObject
{
    /// <summary>Stable Part A scan identifier in scan::{Guid} format.</summary>
    [Required, RegularExpression(@"scan::[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    public string Id { get; set; } = "";

    /// <summary>Tracking identifier in APX-12345678 format.</summary>
    [Required, RegularExpression(@"APX-[0-9]{8}")]
    public string TrackingNumber { get; set; } = "";

    /// <summary>Courier event: Pickup, InTransit, Delivered or Failed.</summary>
    [Required, RegularExpression("^(Pickup|InTransit|Delivered|Failed)$")]
    public string ScanType { get; set; } = "";

    /// <summary>Fictional courier identifier; Part D adds authenticated ownership checks.</summary>
    [Required, MaxLength(100)]
    public string CourierId { get; set; } = "";

    /// <summary>Fictional location where the event was recorded.</summary>
    [Required, MaxLength(200)]
    public string Location { get; set; } = "";
    /// <summary>Event timestamp; required and cannot be in the future.</summary>
    public DateTimeOffset ScannedAtUtc { get; set; }

    /// <summary>Optional scan notes, maximum 2000 characters.</summary>
    [MaxLength(2000)]
    public string Notes { get; set; } = "";

    /// <summary>Shipment API contract or supporting operation.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (ScannedAtUtc == default || ScannedAtUtc > DateTimeOffset.UtcNow)
            yield return new("Scan time is required and cannot be in the future.", [nameof(ScannedAtUtc)]);
    }

    /// <summary>Shipment API contract or supporting operation.</summary>
    public ScanInput ToInput() => new("scan::" + Guid.Parse(Id[6..]).ToString("D"), TrackingNumber, Enum.Parse<CourierScanType>(ScanType), CourierId, Location, ScannedAtUtc, Notes ?? "");
}

/// <summary>Contract for ShipmentV1.</summary>
public sealed record ShipmentV1(string TrackingNumber, decimal WeightKg, string RecipientName, string RecipientPhone, string Address, string City, DeliveryStatus Status);
/// <summary>Contract for RecipientResponse.</summary>
public sealed record RecipientResponse(string Name, string Phone, string Address, string City);
/// <summary>Contract for LatestScanResponse.</summary>
public sealed record LatestScanResponse(string ScanType, string Location, DateTimeOffset ScannedAtUtc);
/// <summary>Contract for ShipmentV2.</summary>
public sealed record ShipmentV2(string TrackingNumber, decimal WeightKg, RecipientResponse Recipient, [property: JsonConverter(typeof(JsonStringEnumConverter<DeliveryStatus>))] DeliveryStatus Status, LatestScanResponse? LatestScan);
/// <summary>A persisted courier scan, with its stable Part A identifier and UTC timestamp.</summary>
public sealed record ScanResponse(string Id, string TrackingNumber, string ScanType, string CourierId, string Location, DateTimeOffset ScannedAtUtc, string Notes);
/// <summary>Contract for PageResponse.</summary>
public sealed record PageResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
/// <summary>Contract for ScanRejectionResponse.</summary>
public sealed record ScanRejectionResponse(string Id, string Reason);
/// <summary>Contract for ScanUploadResponse.</summary>
public sealed record ScanUploadResponse(int Accepted, int DuplicatesSkipped, int Rejected, IReadOnlyList<string> AcceptedScanIds, IReadOnlyList<string> DuplicateScanIds, IReadOnlyList<ScanRejectionResponse> Rejections);
internal static class ResponseMapping
{
    /// <summary>Shipment API contract or supporting operation.</summary>
    public static ShipmentV1 V1(ShipmentView s) => new(s.TrackingNumber, s.WeightKg, s.RecipientName, s.RecipientPhone, s.Address, s.City, s.Status);
    /// <summary>Shipment API contract or supporting operation.</summary>
    public static ShipmentV2 V2(ShipmentView s) => new(s.TrackingNumber, s.WeightKg, new(s.RecipientName, s.RecipientPhone, s.Address, s.City), s.Status, s.LatestScan is { } l ? new(l.ScanType.ToString(), l.Location, l.ScannedAtUtc) : null);
    /// <summary>Shipment API contract or supporting operation.</summary>
    public static ScanResponse Scan(ScanView s) => new(s.Id, s.TrackingNumber, s.ScanType.ToString(), s.CourierId, s.Location, s.ScannedAtUtc, s.Notes);
    /// <summary>Shipment API contract or supporting operation.</summary>
    public static ScanUploadResponse Upload(ScanUploadResult r) => new(r.Accepted, r.DuplicatesSkipped, r.Rejected, r.AcceptedScanIds, r.DuplicateScanIds, r.Rejections.Select(x => new ScanRejectionResponse(x.Id, x.Reason)).ToArray());
}

/// <summary>Validates bulk scan size and rejects null list elements before action execution.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ValidScanBatchAttribute : ValidationAttribute
{
    /// <summary>Checks that the upload contains one to one hundred non-null scan requests.</summary>
    public override bool IsValid(object? value) => value is List<ScanUploadRequest> scans && scans.Count is >= 1 and <= 100 && scans.All(s => s is not null);
    /// <summary>Explains the bulk upload shape requirement.</summary>
    public override string FormatErrorMessage(string name) => "Supply between 1 and 100 non-null scans.";
}
