using ApexPost.Domain.Entities;
using ApexPost.Domain.ValueObjects;
using ApexPost.Infrastructure.Persistence.Entities;

namespace ApexPost.Infrastructure.Persistence.Mapping;
public static class IdentityVerificationMapper
{
    public static IdentityVerificationRecord ToRecord(IdentityVerification value) => new()
    {
        Id = value.Id,
        DocumentType = value.Document.DocumentType,
        MaskedNumber = value.Document.MaskedNumber,
        Fingerprint = value.Document.Fingerprint,
        IsVerified = value.IsVerified,
        Method = value.Method
    };
    public static IdentityVerification ToDomain(IdentityVerificationRecord value) => new(value.Id, IdentityDocument.RestoreMasked(value.DocumentType, value.MaskedNumber, value.Fingerprint), value.IsVerified, value.Method);
}
