using ApexPost.Domain.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPost.Infrastructure.Delivery;
public sealed class TrackedShipmentConfiguration : IEntityTypeConfiguration<TrackedShipment>
{
    public void Configure(EntityTypeBuilder<TrackedShipment> b)
    {
        b.ToTable("TrackedShipments", t =>
        {
            t.HasCheckConstraint("CK_TrackedShipment_Weight", "[WeightKg] BETWEEN 0.1 AND 70");
            t.HasCheckConstraint("CK_TrackedShipment_Status", "[Status] IN ('Created','PickedUp','InTransit','OutForDelivery','Delivered','Failed')");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TrackingNumber).HasMaxLength(12).IsUnicode(false);
        b.HasIndex(x => x.TrackingNumber).IsUnique();
        b.Property(x => x.WeightKg).HasPrecision(18, 6);
        b.Property(x => x.RecipientName).HasMaxLength(120);
        b.Property(x => x.RecipientPhone).HasMaxLength(25).IsUnicode(false);
        b.Property(x => x.Address).HasMaxLength(300);
        b.Property(x => x.City).HasMaxLength(120);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        b.HasIndex(x => new { x.Status, x.City });
        b.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();
    }
}

public sealed class CourierScanConfiguration : IEntityTypeConfiguration<CourierScanRecord>
{
    public void Configure(EntityTypeBuilder<CourierScanRecord> b)
    {
        b.ToTable("CourierScans", t => t.HasCheckConstraint("CK_CourierScan_Type", "[ScanType] IN ('Pickup','InTransit','Delivered','Failed')"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(42).IsUnicode(false).ValueGeneratedNever();
        b.Property(x => x.ScanType).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.CourierId).HasMaxLength(100);
        b.Property(x => x.Location).HasMaxLength(200);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.HasOne<TrackedShipment>().WithMany().HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.ShipmentId, x.ScannedAtUtc });
    }
}
