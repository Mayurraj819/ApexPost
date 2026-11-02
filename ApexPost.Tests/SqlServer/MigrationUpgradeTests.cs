using ApexPost.Domain.Entities;
using ApexPost.Domain.Enums;
using ApexPost.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ApexPost.Tests.SqlServer;

[Collection("SqlServer"), Trait("Category", "SqlServer")]
public sealed class MigrationUpgradeTests(SqlServerFixture fixture)
{
    [SqlServerFact]
    public async Task Existing_ordinal_rows_keep_their_meaning_after_upgrade_and_downgrade()
    {
        // Separate database: never downgrade the shared fixture or development database.
        var builder = new SqlConnectionStringBuilder(fixture.Connection)
        { InitialCatalog = "ApexPost_Test_Upgrade_" + Guid.NewGuid().ToString("N") };
        await using var db = new ApexPostDbContext(new DbContextOptionsBuilder<ApexPostDbContext>()
            .UseSqlServer(builder.ConnectionString, sql => sql.EnableRetryOnFailure()).Options);
        const string previous = "20261030125213_AddMeasuredWorkloadIndexes";
        try
        {
            await db.GetService<IMigrator>().MigrateAsync(previous);
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @Region uniqueidentifier=NEWID(), @Branch uniqueidentifier=NEWID(),
                        @Counter uniqueidentifier=NEWID(), @Staff uniqueidentifier=NEWID(), @Product uniqueidentifier=NEWID();
                INSERT dbo.Regions(Id,Code,Name) VALUES(@Region,'UPGRADE','Upgrade region');
                INSERT dbo.Roles(Id,Name) VALUES(0,'CounterClerk');
                INSERT dbo.ProductCategories(Id,Name) VALUES(0,'OfficeSupplies');
                INSERT dbo.Branches(Id,Code,Name,Address,IsActive,OpeningHours_OpensAt,OpeningHours_ClosesAt,RegionId)
                    VALUES(@Branch,'UPGRADE','Upgrade branch','Fictional address',1,'00:00','23:59',@Region);
                INSERT dbo.Counters(Id,BranchId,Number,Name) VALUES(@Counter,@Branch,1,'Counter');
                INSERT dbo.Staff(Id,BranchId,EmployeeNumber,FullName,Email,Role,IsActive)
                    VALUES(@Staff,@Branch,'UPGRADE','Fictional staff','upgrade@example.com',0,1);
                INSERT dbo.Products(Id,Sku,Name,Category,UnitPriceAmount,UnitPriceCurrency)
                    VALUES(@Product,'UPGRADE','Product',0,10,'PHP');
                INSERT dbo.CounterTransactions(Id,BranchId,CounterId,StaffId,OccurredAt,Status)
                    SELECT NEWID(),@Branch,@Counter,@Staff,SYSUTCDATETIME(),v.Status FROM (VALUES(0),(1),(2),(3)) AS v(Status);
                INSERT dbo.TransactionLines(Id,TransactionId,Type,ProductId,Code,Quantity,UnitPriceAmount,UnitPriceCurrency)
                    SELECT NEWID(),t.Id,v.Type,CASE WHEN v.Type=0 THEN @Product ELSE NULL END,'UPGRADE',1,10,'PHP'
                    FROM dbo.CounterTransactions AS t CROSS JOIN (VALUES(0),(1),(2)) AS v(Type);
                """);
            await db.Database.MigrateAsync();
            Assert.Equal(Enum.GetValues<TransactionStatus>().Order(),
                (await db.Set<Transaction>().AsNoTracking().Select(t => t.Status).ToArrayAsync()).Order());
            Assert.Equal(12, await db.Set<TransactionLine>().CountAsync());
            Assert.Equal(3, await db.Set<TransactionLine>().Select(l => l.Type).Distinct().CountAsync());
            var procedures = new ApexPost.Infrastructure.Queries.SqlOperations(db, new EfUnitOfWork(db));
            var sales = await procedures.DailySalesAsync("UPGRADE", DateOnly.FromDateTime(DateTime.UtcNow), CancellationToken.None);
            Assert.Equal(10m, Assert.Single(sales).Retail);
            Assert.Equal(20m, sales[0].Services);
            await db.GetService<IMigrator>().MigrateAsync(previous);
            var statuses = await db.Database.SqlQuery<int>($"SELECT Status AS Value FROM dbo.CounterTransactions").ToArrayAsync();
            Assert.Equal(new[] { 0, 1, 2, 3 }, statuses.Order());
            await db.Database.MigrateAsync();
            Assert.Equal(4, await db.Set<Transaction>().CountAsync());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
