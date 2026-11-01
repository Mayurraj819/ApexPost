using ApexPost.Domain.Entities;
using ApexPost.Infrastructure.Persistence.Entities;
using ApexPost.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPost.Infrastructure.Persistence.Configurations;
public sealed class AuditRecordConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> b)
    {
        MappingHelpers.Key(b, "AuditEntries");
        b.Property(x => x.Controller).HasMaxLength(100);
        b.Property(x => x.Action).HasMaxLength(100);
        b.Property(x => x.StaffId).HasMaxLength(100);
        b.Property(x => x.Role).HasMaxLength(40);
        b.Property(x => x.BranchId).HasMaxLength(40);
        b.Property(x => x.CorrelationId).HasMaxLength(100);
        b.Property(x => x.SafeRoutes).HasMaxLength(1000);
        b.Property(x => x.Outcome).HasMaxLength(40);
        b.Property(x => x.ExceptionType).HasMaxLength(200);
        MappingHelpers.Check(b, "CK_Audit_Status", "[StatusCode] BETWEEN 100 AND 599");
    }
}
