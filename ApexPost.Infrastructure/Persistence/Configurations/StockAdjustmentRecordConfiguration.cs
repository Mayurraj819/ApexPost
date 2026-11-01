using ApexPost.Domain.Entities;
using ApexPost.Infrastructure.Persistence.Entities;
using ApexPost.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPost.Infrastructure.Persistence.Configurations;
public sealed class StockAdjustmentRecordConfiguration : IEntityTypeConfiguration<StockAdjustmentRecord>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentRecord> b)
    {
        MappingHelpers.Key(b, "StockAdjustments");
        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.ChangedBy).HasMaxLength(100);
        b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.NoAction);
        MappingHelpers.Check(b, "CK_Adjustment_Delta", "[Delta] <> 0");
    }
}
