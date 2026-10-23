using ApexPost.Domain.ValueObjects;

namespace ApexPost.Application.Contracts;

public sealed record MoneyOrderRequest(
    Guid BranchId,
    Guid StaffId,
    Money Amount,
    string PayeeName,
    IdentityDocument IdentityDocument,
    DateTimeOffset? OccurredAt = null);

public sealed record MoneyOrderResult(Guid Reference, Guid BranchId, Guid StaffId, Money Amount, string PayeeName, string IdentityFingerprint, DateTimeOffset CreatedAt);
