using ApexPost.Domain.Entities;
using ApexPost.Infrastructure.Persistence.Entities;
using ApexPost.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPost.Infrastructure.Persistence.Configurations;
public sealed class IdentityVerificationRecordConfiguration : IEntityTypeConfiguration<IdentityVerificationRecord>
{
    public void Configure(EntityTypeBuilder<IdentityVerificationRecord> b)
    {
        MappingHelpers.Key(b, "IdentityVerifications");
        b.Property(x => x.DocumentType).HasMaxLength(40);
        b.Property(x => x.MaskedNumber).HasMaxLength(7).IsUnicode(false);
        b.Property(x => x.Fingerprint).HasMaxLength(64).IsUnicode(false);
        b.Property(x => x.Method).HasMaxLength(60);
        MappingHelpers.Check(b, "CK_Identity_Masked", "LEFT([MaskedNumber],3) = '***' AND LEN([MaskedNumber]) BETWEEN 4 AND 7");
    }
}
