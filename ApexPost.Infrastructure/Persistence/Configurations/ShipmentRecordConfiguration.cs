using ApexPost.Domain.Entities;
using ApexPost.Infrastructure.Persistence.Entities;
using ApexPost.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPost.Infrastructure.Persistence.Configurations;
public sealed class ShipmentRecordConfiguration : IEntityTypeConfiguration<ShipmentRecord>
{
    public void Configure(EntityTypeBuilder<ShipmentRecord> b)
    {
        MappingHelpers.Key(b, "Shipments");
        b.ComplexProperty(x => x.Charge, m => MappingHelpers.Money(m, "Charge"));
        b.Property(x => x.Weight).HasConversion<WeightConverter>().HasPrecision(18, 3);
        b.Property(x => x.RecipientName).HasMaxLength(120);
        b.HasOne<Transaction>().WithMany().HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<ShippingZoneRecord>().WithMany().HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.NoAction);
        b.OwnsOne(x => x.Address, a =>
        {
            a.Property(x => x.Street).HasMaxLength(200);
            a.Property(x => x.City).HasMaxLength(100);
            a.Property(x => x.PostalCode).HasMaxLength(20).IsUnicode(false);
        });
        MappingHelpers.Check(b, "CK_Shipment_Weight", "[Weight] > 0 AND [Weight] <= 30000");
    }
}
