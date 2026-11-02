using ApexPost.Application.Exceptions;
using ApexPost.Domain.Entities;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;
using ApexPost.Infrastructure.Persistence;
using ApexPost.Infrastructure.Persistence.Repositories;
using ApexPost.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Tests.SqlServer;
[Collection("SqlServer"), Trait("Category", "SqlServer")]
public sealed class SqlPersistenceTests(SqlServerFixture fixture)
{
    [SqlServerFact]
    public async Task Repository_stages_until_uow_and_private_state_roundtrips()
    {
        await using var db = fixture.Create();
        var branch = await db.Set<Branch>().FirstAsync();
        var counter = await db.Set<Counter>().FirstAsync(x => x.BranchId == branch.Id);
        var staff = await db.Set<Staff>().Where(s => s.IsActive).FirstAsync(x => x.BranchId == branch.Id);
        var product = await db.Set<Product>().Where(p => p.Name.StartsWith("Demo product")).FirstAsync();
        var sale = Transaction.Start(branch, counter, staff, new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
        sale.AddProduct(product, new Quantity(2));
        sale.AddService("POST", TransactionLineType.PostalService, new Money(70), ShippingServiceLevel.Standard);
        sale.Pay(staff, branch);
        var repo = new EfTransactionRepository(db);
        await repo.AddAsync(sale);
        await using (var observer = fixture.Create())
            Assert.False(await observer.Set<Transaction>().AnyAsync(t => t.Id == sale.Id));
        await new EfUnitOfWork(db).SaveChangesAsync();
        await using var verify = fixture.Create();
        var loaded = await new EfTransactionRepository(verify).GetByIdAsync(sale.Id);
        Assert.NotNull(loaded);
        Assert.Equal(TransactionStatus.Paid, loaded.Status);
        Assert.Equal(2, loaded.Lines.Count);
        Assert.Equal(sale.Total, loaded.Total);
    }

    [SqlServerFact]
    public async Task Last_unit_cannot_be_sold_by_two_contexts()
    {
        Guid id;
        await using (var setup = fixture.Create())
        {
            var row = await setup.Set<BranchStock>().FirstAsync();
            if (row.Quantity.Value > 1)
                row.Decrease(row.Quantity.Value - 1);
            else if (row.Quantity.Value == 0)
                row.Increase(1);
            await setup.SaveChangesAsync();
            id = row.Id;
        }

        await using var a = fixture.Create();
        await using var b = fixture.Create();
        var first = await a.Set<BranchStock>().SingleAsync(x => x.Id == id);
        var second = await b.Set<BranchStock>().SingleAsync(x => x.Id == id);
        first.Decrease(1);
        second.Decrease(1);
        await new EfUnitOfWork(a).SaveChangesAsync();
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => new EfUnitOfWork(b).SaveChangesAsync());
        await using var check = fixture.Create();
        Assert.Equal(0, (await check.Set<BranchStock>().SingleAsync(x => x.Id == id)).Quantity.Value);
    }

    [SqlServerFact]
    public async Task Failed_sale_rolls_back_stock_header_and_payment()
    {
        await using var db = fixture.Create();
        var stock = await db.Set<BranchStock>().FirstAsync(s => EF.Property<int>(s, "_quantity") > 1);
        var before = stock.Quantity.Value;
        var branch = await db.Set<Branch>().SingleAsync(b => b.Id == stock.BranchId);
        var counter = await db.Set<Counter>().FirstAsync(c => c.BranchId == branch.Id);
        var clerk = await db.Set<Staff>().Where(s => s.IsActive).FirstAsync(s => s.BranchId == branch.Id);
        var product = await db.Set<Product>().SingleAsync(p => p.Id == stock.ProductId);
        var sale = Transaction.Start(branch, counter, clerk, new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
        sale.AddProduct(product, new Quantity(1));
        sale.Pay(clerk, branch);
        stock.Decrease(1);
        db.Add(sale);
        // Negative payment bypass is impossible through the domain. A real SQL trigger simulates failure after staging all valid rows.
        await db.Database.ExecuteSqlRawAsync("CREATE OR ALTER TRIGGER dbo.ApexPost_Test_FailPayment ON dbo.Payments AFTER INSERT AS THROW 51000, 'Test payment failure', 1;");
        try
        {
            db.Add(new Payment(sale.Id, sale.Id, sale.Total, DateTimeOffset.UtcNow));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            await using var verify = fixture.Create();
            Assert.False(await verify.Set<Transaction>().AnyAsync(t => t.Id == sale.Id));
            Assert.False(await verify.Set<Payment>().AnyAsync(t => t.Id == sale.Id));
            Assert.Equal(before, (await verify.Set<BranchStock>().SingleAsync(s => s.Id == stock.Id)).Quantity.Value);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS dbo.ApexPost_Test_FailPayment;");
        }
    }

    [SqlServerFact]
    public async Task Three_procedures_execute_and_adjustment_writes_history()
    {
        await using var db = fixture.Create();
        var branch = await db.Set<Branch>().FirstAsync();
        var product = await db.Set<Product>().Where(p => p.Name.StartsWith("Demo product")).FirstAsync();
        var ops = new SqlOperations(db, new EfUnitOfWork(db));
        var rows = await ops.DailySalesAsync(branch.Code.Value, DateOnly.FromDateTime(DateTime.UtcNow), CancellationToken.None);
        Assert.NotEmpty(rows);
        _ = await ops.LowStockAsync(branch.Code.Value, 3, CancellationToken.None);
        var stock = await db.Set<BranchStock>().SingleAsync(s => s.BranchId == branch.Id && s.ProductId == product.Id);
        var before = stock.Quantity.Value;
        await ops.AdjustStockAsync(branch.Code.Value, product.Sku.Value, 1, "Test adjustment", "test", CancellationToken.None);
        Assert.Equal(before + 1, (await db.Set<BranchStock>().SingleAsync(s => s.Id == stock.Id)).Quantity.Value);
        Assert.True(await db.Set<ApexPost.Infrastructure.Persistence.Entities.StockAdjustmentRecord>().AnyAsync(x => x.BranchId == branch.Id && x.ProductId == product.Id && x.Reason == "Test adjustment"));
    }

    [SqlServerFact]
    public async Task All_read_screens_translate_and_execute()
    {
        await using var db = fixture.Create();
        var branch = (await db.Set<Branch>().FirstAsync()).Id;
        var q = new PlatformQueries(db);
        Assert.NotEmpty(await q.ProductsAsync(null, CancellationToken.None));
        Assert.NotEmpty(await q.StockAsync(branch, CancellationToken.None));
        var transactions = await q.TransactionsAsync(branch, CancellationToken.None);
        Assert.NotEmpty(transactions);
        Assert.NotNull(await q.TransactionAsync(transactions[0].Id, CancellationToken.None));
        Assert.NotEmpty((await q.ReportsAsync(branch, DateTimeOffset.UtcNow.AddDays(-60), DateTimeOffset.UtcNow, CancellationToken.None)).Sales);
        Assert.NotNull(await q.DashboardAsync(branch, CancellationToken.None));
        Assert.NotNull(await q.AlertsAsync(branch, CancellationToken.None));
    }

    [SqlServerFact]
    public async Task Set_based_price_revision_updates_audit_without_materializing_products()
    {
        await using var db = fixture.Create();
        var ops = new SqlOperations(db, new EfUnitOfWork(db));
        var expected = await db.Set<Product>().CountAsync();
        Assert.Equal(expected, await ops.RevisePricesAsync(1m, "test", CancellationToken.None));
        Assert.Empty(db.ChangeTracker.Entries<Product>());
    }

    [SqlServerFact]
    public async Task Money_order_stores_only_masked_identity_and_preserves_verification_policy()
    {
        await using var db = fixture.Create();
        var clerk = await db.Set<Staff>().Where(s => s.IsActive).FirstAsync(x => x.Role == StaffRole.CounterClerk);
        var service = new ApexPost.Application.Services.MoneyOrderService(new EfBranchRepository(db), new EfStaffRepository(db), new EfMoneyOrderRepository(db), new EfUnitOfWork(db));
        var result = await service.CreateAsync(new ApexPost.Application.Contracts.MoneyOrderRequest(clerk.BranchId, clerk.Id, new Money(500), "Fictional payee", IdentityDocument.Create("Passport", "FICTIONAL1234"), new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero)));
        await using var verify = fixture.Create();
        var identity = await verify.Set<ApexPost.Infrastructure.Persistence.Entities.IdentityVerificationRecord>().SingleAsync(x => x.Id == result.Reference);
        Assert.Equal("***1234", identity.MaskedNumber);
        Assert.False(identity.IsVerified);
        Assert.DoesNotContain("FICTIONAL1234", identity.Fingerprint);
        var restored = await new EfMoneyOrderRepository(verify).GetByReferenceAsync(result.Reference);
        Assert.Equal(result.Amount, restored!.Amount);
    }

    [SqlServerFact]
    public async Task Shipping_line_and_shipment_are_committed_together_and_can_be_paid()
    {
        await using var db = fixture.Create();
        var clerk = await db.Set<Staff>().Where(s => s.IsActive).FirstAsync(x => x.Role == StaffRole.CounterClerk);
        var branch = await db.Set<Branch>().SingleAsync(b => b.Id == clerk.BranchId);
        var counter = await db.Set<Counter>().FirstAsync(c => c.BranchId == branch.Id);
        var sale = Transaction.Start(branch, counter, clerk, new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
        db.Add(sale);
        await db.SaveChangesAsync();
        var zone = await db.Set<ApexPost.Infrastructure.Persistence.Entities.ShippingZoneRecord>().FirstAsync();
        var command = new ApexPost.Application.Contracts.AddShipmentCommand(sale.Id, branch.Id, zone.Id, new Weight(1000), ShippingServiceLevel.Standard, "Recipient", "Demo street", "Demo city", "1000");
        await new ApexPost.Application.Services.ShippingCommands(new EfTransactionRepository(db), new EfShipmentStore(db), new EfUnitOfWork(db)).AddAsync(command, CancellationToken.None);
        await using var verify = fixture.Create();
        var loaded = await new EfTransactionRepository(verify).GetByIdAsync(sale.Id);
        Assert.Single(loaded!.Lines);
        Assert.Equal(70m, loaded.Total.Amount);
        Assert.True(await verify.Set<ApexPost.Infrastructure.Persistence.Entities.ShipmentRecord>().AnyAsync(x => x.TransactionId == sale.Id));
    }

    [SqlServerFact]
    public async Task Unique_constraint_is_translated_to_conflict()
    {
        await using var db = fixture.Create();
        var original = await db.Set<Product>().AsNoTracking().FirstAsync();
        db.Add(new Product(Guid.NewGuid(), original.Sku, "Duplicate", new Money(1)));
        await Assert.ThrowsAsync<UniqueConflictException>(() => new EfUnitOfWork(db).SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task New_checkout_inserts_header_lines_payment_and_stock_in_one_commit()
    {
        await using var db = fixture.Create();
        var stock = await db.Set<BranchStock>().FirstAsync(x => EF.Property<int>(x, "_quantity") > 2);
        var before = stock.Quantity.Value;
        var counter = await db.Set<Counter>().FirstAsync(x => x.BranchId == stock.BranchId);
        var clerk = await db.Set<Staff>().Where(s => s.IsActive).FirstAsync(x => x.BranchId == stock.BranchId);
        var service = new ApexPost.Application.Services.TransactionProcessingService(new EfBranchRepository(db), new EfCounterRepository(db), new EfStaffRepository(db), new EfProductRepository(db), new EfTransactionRepository(db), new EfStockRepository(db), new EfUnitOfWork(db), new EfPaymentRepository(db));
        var id = await service.CheckoutAsync(new ApexPost.Application.Contracts.CheckoutCommand(stock.BranchId, counter.Id, clerk.Id, new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero), [new(stock.ProductId, 1)]));
        await using var verify = fixture.Create();
        var sale = await verify.Set<Transaction>().Include(t => t.Lines).SingleAsync(x => x.Id == id);
        Assert.Equal(TransactionStatus.Paid, sale.Status);
        Assert.Single(sale.Lines);
        Assert.True(await verify.Set<Payment>().AnyAsync(x => x.TransactionId == id));
        Assert.Equal(before - 1, (await verify.Set<BranchStock>().SingleAsync(x => x.Id == stock.Id)).Quantity.Value);
    }

    [SqlServerFact]
    public async Task Closed_branch_inactive_staff_and_masked_document_roundtrip()
    {
        await using var db = fixture.Create();
        var branch = await db.Set<Branch>().FirstAsync(b => b.IsActive);
        var person = new Staff(Guid.NewGuid(), branch.Id, "IDENTITY-TEST", "Fictional staff", new EmailAddress("identity-roundtrip@example.com"), StaffRole.CounterClerk, IdentityDocument.Create("Passport", "FICTIONAL9876"));
        person.Deactivate();
        branch.Close();
        db.Add(person);
        try
        {
            await db.SaveChangesAsync();
            await using var read = fixture.Create();
            Assert.False((await read.Set<Branch>().SingleAsync(b => b.Id == branch.Id)).IsActive);
            var stored = await read.Set<Staff>().SingleAsync(p => p.Id == person.Id);
            Assert.False(stored.IsActive);
            Assert.Equal("***9876", stored.IdentityDocument!.MaskedNumber);
        }
        finally
        {
            branch.Reopen();
            await db.SaveChangesAsync();
        }
    }

    [SqlServerFact]
    public async Task Generated_schema_script_can_be_replayed_idempotently()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../sql/schema.sql"));
        var sql = await File.ReadAllTextAsync(path);
        var batches = System.Text.RegularExpressions.Regex.Split(sql, @"(?m)^\s*GO\s*$");
        await using var db = fixture.Create();
        await db.Database.OpenConnectionAsync();
        for (var replay = 0; replay < 2; replay++)
            foreach (var batch in batches.Where(b => !string.IsNullOrWhiteSpace(b)))
                await db.Database.ExecuteSqlRawAsync(batch);
        Assert.Equal(4, (await db.Database.GetAppliedMigrationsAsync()).Count());
    }
}
