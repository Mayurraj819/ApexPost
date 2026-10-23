using ApexPost.Domain.ValueObjects;

namespace ApexPost.Application.Contracts;

public sealed record IdentityVerificationRequest(IdentityDocument Document, string ExpectedFingerprint);

public sealed record IdentityVerificationResult(bool IsVerified, string MaskedNumber, string DocumentType);
