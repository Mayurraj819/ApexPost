using ApexPost.Domain.Entities;
using ApexPost.Domain.ValueObjects;
using ApexPost.Infrastructure.Persistence.Converters;
using ApexPost.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPost.Infrastructure.Persistence.Configurations;
public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> b)
    {
        MappingHelpers.Key(b, "Branches");
        b.Property(x => x.Code).HasConversion<BranchCodeConverter>().HasMaxLength(40).IsUnicode(false);
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Name).HasMaxLength(120);
        b.Property(x => x.Address).HasMaxLength(300);
        b.Property(x => x.IsActive);
        b.OwnsOne(x => x.OpeningHours, o =>
        {
            o.Property(x => x.OpensAt);
            o.Property(x => x.ClosesAt);
        });
        b.Property<Guid>("RegionId");
        b.HasOne<RegionRecord>().WithMany().HasForeignKey("RegionId").OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class CounterConfiguration : IEntityTypeConfiguration<Counter>
{
    public void Configure(EntityTypeBuilder<Counter> b)
    {
        MappingHelpers.Key(b, "Counters");
        b.Property(x => x.BranchId);
        b.Property(x => x.Number);
        b.Property(x => x.Name).HasMaxLength(120);
        b.HasIndex(x => new { x.BranchId, x.Number }).IsUnique();
        b.HasAlternateKey(x => new { x.Id, x.BranchId });
        b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.NoAction);
        MappingHelpers.Check(b, "CK_Counter_Number", "[Number] > 0");
    }
}

public sealed class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> b)
    {
        MappingHelpers.Key(b, "Staff");
        b.Property(x => x.BranchId);
        b.Property(x => x.EmployeeNumber).HasMaxLength(40).IsUnicode(false);
        b.Property(x => x.FullName).HasMaxLength(120);
        b.Property(x => x.Email).HasConversion(v => v.Value, v => new EmailAddress(v)).HasMaxLength(254).IsUnicode(false);
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.Role);
        b.Property(x => x.IsActive);
        b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<RoleRecord>().WithMany().HasForeignKey(x => x.Role).OnDelete(DeleteBehavior.NoAction);
        b.OwnsOne(x => x.IdentityDocument, o =>
        {
            o.Property(x => x.DocumentType).HasMaxLength(40);
            o.Property(x => x.MaskedNumber).HasMaxLength(7).IsUnicode(false);
            o.Property(x => x.Fingerprint).HasMaxLength(64).IsUnicode(false);
        });
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        MappingHelpers.Key(b, "Products");
        b.Property(x => x.Sku).HasConversion<SkuConverter>().HasMaxLength(40).IsUnicode(false);
        b.HasIndex(x => x.Sku).IsUnique();
        b.Property(x => x.Name).HasMaxLength(120);
        b.Property(x => x.Category);
        b.Property(x => x.Weight).HasConversion<WeightConverter>().HasPrecision(18, 3);
        b.ComplexProperty(x => x.UnitPrice, m => MappingHelpers.Money(m, "UnitPrice"));
        b.HasOne<ProductCategoryRecord>().WithMany().HasForeignKey(x => x.Category).OnDelete(DeleteBehavior.NoAction);
        MappingHelpers.Check(b, "CK_Product_Price", "[UnitPriceAmount] >= 0");
        MappingHelpers.Check(b, "CK_Product_Weight", "[Weight] IS NULL OR [Weight] >= 0");
    }
}

public sealed class StockConfiguration : IEntityTypeConfiguration<BranchStock>
{
    public void Configure(EntityTypeBuilder<BranchStock> b)
    {
        MappingHelpers.Key(b, "BranchStock");
        b.Property(x => x.BranchId);
        b.Property(x => x.ProductId);
        b.Ignore(x => x.Quantity);
        b.Property<int>("_quantity").HasField("_quantity").HasColumnName("Quantity");
        b.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();
        b.HasIndex(x => new { x.BranchId, x.ProductId }).IsUnique();
        b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.NoAction);
        MappingHelpers.Check(b, "CK_Stock_Quantity", "[Quantity] >= 0");
    }
}

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> b)
    {
        MappingHelpers.Key(b, "CounterTransactions");
        b.Property(x => x.BranchId);
        b.Property(x => x.CounterId);
        b.Property(x => x.StaffId);
        b.Property(x => x.OccurredAt);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsUnicode(false);
        b.Property(x => x.StatusReason).HasMaxLength(500);
        b.Ignore(x => x.Total);
        b.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();
        b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Counter>().WithMany().HasForeignKey(x => new { x.CounterId, x.BranchId }).HasPrincipalKey(x => new { x.Id, x.BranchId }).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Staff>().WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.NoAction);
        b.HasMany(x => x.Lines).WithOne().HasForeignKey("TransactionId").OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
        MappingHelpers.Check(b, "CK_Transaction_Status", "[Status] IN ('Open','Paid','Voided','Refunded')");
    }
}

public sealed class TransactionLineConfiguration : IEntityTypeConfiguration<TransactionLine>
{
    public void Configure(EntityTypeBuilder<TransactionLine> b)
    {
        MappingHelpers.Key(b, "TransactionLines");
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(24).IsUnicode(false);
        b.Property(x => x.ProductId);
        b.Property(x => x.Code).HasMaxLength(40).IsUnicode(false);
        b.Ignore(x => x.Quantity);
        b.Property<int>("_quantity").HasField("_quantity").HasColumnName("Quantity");
        b.Property(x => x.ShippingLevel);
        b.Ignore(x => x.Total);
        b.ComplexProperty(x => x.UnitPrice, m => MappingHelpers.Money(m, "UnitPrice"));
        b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.NoAction);
        MappingHelpers.Check(b, "CK_Line_Quantity", "[Quantity] > 0");
        MappingHelpers.Check(b, "CK_Line_Type", "[Type] IN ('RetailProduct','PostalService','FinancialService','ParcelShipping')");
        MappingHelpers.Check(b, "CK_Line_Product", "[Type] <> 'RetailProduct' OR [ProductId] IS NOT NULL");
        MappingHelpers.Check(b, "CK_Line_Price", "[UnitPriceAmount] >= 0");
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        MappingHelpers.Key(b, "Payments");
        b.Property(x => x.TransactionId);
        b.Property(x => x.PaidAtUtc);
        b.ComplexProperty(x => x.Amount, m => MappingHelpers.Money(m, "Paid"));
        b.HasIndex(x => x.TransactionId).IsUnique();
        b.HasOne<Transaction>().WithMany().HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.NoAction);
        MappingHelpers.Check(b, "CK_Payment_Amount", "[PaidAmount] > 0");
    }
}
