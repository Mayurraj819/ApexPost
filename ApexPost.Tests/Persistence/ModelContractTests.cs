using ApexPost.Domain.Entities;
using ApexPost.Domain.ValueObjects;
using ApexPost.Infrastructure.Persistence;
using ApexPost.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Tests.Persistence;
[Trait("Category", "Unit")]
public sealed class ModelContractTests
{
    internal static ApexPostDbContext Context() => new(new DbContextOptionsBuilder<ApexPostDbContext>().UseSqlServer("Server=localhost;Database=ApexPost_ModelOnly;Integrated Security=true;TrustServerCertificate=true").Options);
    [Fact]
    public void Model_has_required_tables_rowversions_and_private_line_collection()
    {
        using var db = Context();
        var tables = db.Model.GetEntityTypes().Select(t => t.GetTableName()).Distinct().ToArray();
        foreach (var table in new[]
        {
            "Branches",
            "Counters",
            "Staff",
            "Roles",
            "Products",
            "ProductCategories",
            "BranchStock",
            "StockAdjustments",
            "ShippingZones",
            "ShippingRates",
            "Shipments",
            "MoneyOrders",
            "IdentityVerifications",
            "CounterTransactions",
            "TransactionLines",
            "Payments",
            "AuditEntries"
        }

        )
            Assert.Contains(table, tables);
        Assert.True(db.Model.FindEntityType(typeof(BranchStock))!.FindProperty("RowVersion")!.IsConcurrencyToken);
        Assert.True(db.Model.FindEntityType(typeof(Transaction))!.FindProperty("RowVersion")!.IsConcurrencyToken);
        Assert.Equal("_lines", db.Model.FindEntityType(typeof(Transaction))!.FindNavigation(nameof(Transaction.Lines))!.FieldInfo!.Name);
    }

    [Fact]
    public void Important_queries_translate_without_connecting_to_a_database()
    {
        using var db = Context();
        var branch = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var stock = db.Set<BranchStock>().AsNoTracking().Where(x => x.BranchId == branch && EF.Property<int>(x, "_quantity") <= 3).Select(x => new { x.ProductId, Quantity = EF.Property<int>(x, "_quantity") }).ToQueryString();
        var sales = (
            from t in db.Set<Transaction>().AsNoTracking()join l in db.Set<TransactionLine>().AsNoTracking()on t.Id equals EF.Property<Guid>(l, "TransactionId")
            where t.OccurredAt >= start
            select new
            {
                t.BranchId,
                Amount = l.UnitPrice.Amount * EF.Property<int>(l, "_quantity")
            }

        ).GroupBy(x => x.BranchId).Select(g => new { g.Key, Total = g.Sum(x => x.Amount) }).ToQueryString();
        var products = db.Set<Product>().AsNoTracking().Select(x => new { x.Name, x.UnitPrice.Amount, x.UnitPrice.Currency }).ToQueryString();
        Assert.Contains("Quantity", stock);
        Assert.Contains("SUM", sales);
        Assert.Contains("UnitPriceAmount", products);
    }

    [Fact]
    public void Masked_identity_mapping_never_reconstructs_a_full_number()
    {
        const string raw = "ABCD12345678";
        var document = IdentityDocument.Create("Passport", raw);
        var original = new IdentityVerification(Guid.NewGuid(), document, false, "FingerprintRecorded");
        var record = ApexPost.Infrastructure.Persistence.Mapping.IdentityVerificationMapper.ToRecord(original);
        var restored = ApexPost.Infrastructure.Persistence.Mapping.IdentityVerificationMapper.ToDomain(record);
        Assert.Equal("***5678", record.MaskedNumber);
        Assert.Equal(original, restored);
        Assert.DoesNotContain(raw, record.Fingerprint);
    }
}
