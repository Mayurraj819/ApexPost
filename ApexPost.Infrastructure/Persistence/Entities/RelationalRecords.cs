using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Infrastructure.Persistence.Entities;
public sealed class RegionRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class RoleRecord
{
    public StaffRole Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class ProductCategoryRecord
{
    public ProductCategory Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class StockAdjustmentRecord
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public int Delta { get; set; }
    public string Reason { get; set; } = "";
    public string ChangedBy { get; set; } = "";
}

public sealed class ShippingZoneRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class ShippingRateRecord
{
    public Guid Id { get; set; }
    public Guid ZoneId { get; set; }
    public ShippingServiceLevel ServiceLevel { get; set; }
    public Money BaseRate { get; set; }
    public decimal PerKilogram { get; set; }
}

public sealed class ShipmentRecord
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid ZoneId { get; set; }
    public ShippingServiceLevel ServiceLevel { get; set; }
    public Weight Weight { get; set; }
    public Money Charge { get; set; }
    public string RecipientName { get; set; } = "";
    public AddressRecord Address { get; set; } = new();
}

public sealed class AddressRecord
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string PostalCode { get; set; } = "";
}

public sealed class MoneyOrderRecord
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid StaffId { get; set; }
    public Money Amount { get; set; }
    public string PayeeName { get; set; } = "";
    public Guid IdentityVerificationId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}

public sealed class IdentityVerificationRecord
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = "";
    public string MaskedNumber { get; set; } = "";
    public string Fingerprint { get; set; } = "";
    public bool IsVerified { get; set; }
    public string Method { get; set; } = "FingerprintRecorded";
}

public sealed class AuditRecord
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string Controller { get; set; } = "";
    public string Action { get; set; } = "";
    public string? StaffId { get; set; }
    public string? Role { get; set; }
    public string? BranchId { get; set; }
    public string CorrelationId { get; set; } = "";
    public int StatusCode { get; set; }
    public double ElapsedMilliseconds { get; set; }
    public string SafeRoutes { get; set; } = "";
    public string Outcome { get; set; } = "";
    public string? ExceptionType { get; set; }
}
