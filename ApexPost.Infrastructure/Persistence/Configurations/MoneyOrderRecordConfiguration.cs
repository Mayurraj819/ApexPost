using ApexPost.Domain.Entities;
using ApexPost.Infrastructure.Persistence.Entities;
using ApexPost.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPost.Infrastructure.Persistence.Configurations;
public sealed class MoneyOrderRecordConfiguration : IEntityTypeConfiguration<MoneyOrderRecord>
{
    public void Configure(EntityTypeBuilder<MoneyOrderRecord> b)
    {
        MappingHelpers.Key(b, "MoneyOrders");
        b.Property(x => x.PayeeName).HasMaxLength(120);
        b.ComplexProperty(x => x.Amount, m => MappingHelpers.Money(m, "Order"));
        b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Staff>().WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<IdentityVerificationRecord>().WithMany().HasForeignKey(x => x.IdentityVerificationId).OnDelete(DeleteBehavior.NoAction);
        MappingHelpers.Check(b, "CK_MoneyOrder_Amount", "[OrderAmount] > 0");
    }
}
