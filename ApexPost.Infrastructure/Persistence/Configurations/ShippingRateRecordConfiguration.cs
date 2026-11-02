using ApexPost.Domain.Entities;
using ApexPost.Infrastructure.Persistence.Entities;
using ApexPost.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPost.Infrastructure.Persistence.Configurations;
public sealed class ShippingRateRecordConfiguration : IEntityTypeConfiguration<ShippingRateRecord>
{
    public void Configure(EntityTypeBuilder<ShippingRateRecord> b)
    {
        MappingHelpers.Key(b, "ShippingRates");
        b.Property(x => x.BaseRate).HasConversion<PhpMoneyConverter>().HasPrecision(18, 2);
        b.Property(x => x.PerKilogram).HasPrecision(18, 2);
        b.HasIndex(x => new { x.ZoneId, x.ServiceLevel }).IsUnique();
        b.HasOne<ShippingZoneRecord>().WithMany().HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.NoAction);
        MappingHelpers.Check(b, "CK_Rate_Amounts", "[BaseRate] > 0 AND [PerKilogram] >= 0");
    }
}
