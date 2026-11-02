using System.Data.Common;
using ApexPost.Domain.Entities;
using ApexPost.Infrastructure.Persistence;
using ApexPost.Infrastructure.Persistence.Entities;
using ApexPost.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ApexPost.Tests.SqlServer;

[Collection("SqlServer"), Trait("Category", "SqlServer")]
public sealed class RetrySafetyTests(SqlServerFixture fixture)
{
    private sealed class CommitFailure : DbTransactionInterceptor
    {
        public bool Injected { get; private set; }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data,
            CancellationToken cancellationToken = default)
        {
            if (!Injected)
            {
                Injected = true;
                throw new TimeoutException("Injected acknowledgement loss after successful COMMIT.");
            }
            return Task.CompletedTask;
        }
    }

    private sealed class UpdateFailure : DbCommandInterceptor
    {
        public int Updates { get; private set; }
        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData data,
            int result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("UPDATE", StringComparison.OrdinalIgnoreCase)
                && command.CommandText.Contains("Products", StringComparison.Ordinal))
            {
                Updates++;
                if (Updates == 1)
                    throw new TimeoutException("Injected failure after UPDATE, before COMMIT.");
            }
            return ValueTask.FromResult(result);
        }
    }

    private ApexPostDbContext Context(IInterceptor interceptor) => new(
        new DbContextOptionsBuilder<ApexPostDbContext>().UseSqlServer(fixture.Connection,
            sql => sql.EnableRetryOnFailure(3, TimeSpan.Zero, null)).AddInterceptors(interceptor).Options);

    [SqlServerFact]
    public async Task Lost_commit_acknowledgement_does_not_adjust_stock_twice()
    {
        var fault = new CommitFailure();
        await using var db = Context(fault);
        var stock = await db.Set<BranchStock>().AsNoTracking().FirstAsync();
        var branch = await db.Set<Branch>().AsNoTracking().SingleAsync(b => b.Id == stock.BranchId);
        var product = await db.Set<Product>().AsNoTracking().SingleAsync(p => p.Id == stock.ProductId);
        var receipts = await db.Set<OperationReceipt>().CountAsync();
        var reason = "Commit verification " + Guid.NewGuid();
        await new SqlOperations(db, new EfUnitOfWork(db)).AdjustStockAsync(
            branch.Code.Value, product.Sku.Value, 1, reason, "test", CancellationToken.None);
        Assert.True(fault.Injected);
        await using var verify = fixture.Create();
        Assert.Equal(stock.Quantity.Value + 1, (await verify.Set<BranchStock>().SingleAsync(s => s.Id == stock.Id)).Quantity.Value);
        Assert.Equal(1, await verify.Set<StockAdjustmentRecord>().CountAsync(s => s.Reason == reason));
        Assert.Equal(receipts + 1, await verify.Set<OperationReceipt>().CountAsync());
    }

    [SqlServerFact]
    public async Task Precommit_failure_rolls_back_before_replaying_price_revision()
    {
        var fault = new UpdateFailure();
        await using var db = Context(fault);
        var original = await db.Set<Product>().AsNoTracking().FirstAsync();
        var receipts = await db.Set<OperationReceipt>().CountAsync();
        var expectedCount = await db.Set<Product>().CountAsync();
        var changed = false;
        try
        {
            var count = await new SqlOperations(db, new EfUnitOfWork(db))
                .RevisePricesAsync(2, "test", CancellationToken.None);
            changed = true;
            Assert.Equal(expectedCount, count);
            Assert.Equal(2, fault.Updates);
            await using var verify = fixture.Create();
            Assert.Equal(original.UnitPrice.Amount * 2,
                (await verify.Set<Product>().SingleAsync(p => p.Id == original.Id)).UnitPrice.Amount);
            Assert.Equal(receipts + 1, await verify.Set<OperationReceipt>().CountAsync());
        }
        finally
        {
            // Restore the shared disposable fixture, including when an assertion fails.
            await using var reset = fixture.Create();
            if (changed)
                await reset.Set<Product>().ExecuteUpdateAsync(s => s.SetProperty(p => p.UnitPrice.Amount,
                p => p.UnitPrice.Amount / 2));
        }
    }
}
