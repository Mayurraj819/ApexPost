using ApexPost.Domain.ValueObjects;

namespace ApexPost.Domain.Entities;
public sealed record IdentityVerification(Guid Id, IdentityDocument Document, bool IsVerified, string Method);
