using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ApexPost.Domain.Entities;
using ApexPost.Infrastructure.Persistence;
using ApexPost.Infrastructure.Persistence.Entities;
using ApexPost.Infrastructure.Seeding;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var root = Directory.GetCurrentDirectory();
if (!File.Exists(Path.Combine(root, "ApexPost.slnx")))
    throw new InvalidOperationException("Run from the solution root.");
var source = Environment.GetEnvironmentVariable("APEXPOST_TEST_SQLSERVER") ?? throw new InvalidOperationException("Set a master SQL Server test connection.");
var builder = new SqlConnectionStringBuilder(source);
if (!string.IsNullOrEmpty(builder.InitialCatalog) && !builder.InitialCatalog.Equals("master", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Base connection must target master.");
builder.InitialCatalog = "ApexPost_Perf_" + Guid.NewGuid().ToString("N");
await using var db = new ApexPostDbContext(new DbContextOptionsBuilder<ApexPostDbContext>().UseSqlServer(builder.ConnectionString).Options);
var folder = Path.Combine(root, "evidence");
Directory.CreateDirectory(Path.Combine(folder, "plans"));
Directory.CreateDirectory(Path.Combine(folder, "generated-sql"));
try
{
    await db.Database.MigrateAsync();
    await new ApexPostSeeder(db).SeedAsync();
    await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync("sql/performance/SeedScale.sql"));
    var branch = await db.Set<Branch>().AsNoTracking().OrderBy(x => x.Code).Select(x => x.Id).FirstAsync();
    var zone = await db.Set<ShippingZoneRecord>().AsNoTracking().Select(x => x.Id).FirstAsync();
    var today = DateTimeOffset.UtcNow;
    var from = today.AddDays(-30);
    await File.WriteAllTextAsync(Path.Combine(folder, "generated-sql/low-stock.sql"), db.Set<BranchStock>().AsNoTracking().Where(x => x.BranchId == branch && EF.Property<int>(x, "_quantity") <= 3).Select(x => new { x.ProductId, Quantity = EF.Property<int>(x, "_quantity") }).ToQueryString());
    await File.WriteAllTextAsync(Path.Combine(folder, "generated-sql/sales.sql"), db.Set<Transaction>().AsNoTracking().Where(t => t.BranchId == branch && t.OccurredAt >= from && t.OccurredAt < today).Select(t => new { t.Id, t.OccurredAt, t.Status }).ToQueryString());
    await File.WriteAllTextAsync(Path.Combine(folder, "generated-sql/shipments.sql"), db.Set<ShipmentRecord>().AsNoTracking().Where(s => s.ZoneId == zone && s.ServiceLevel == 0).Select(s => new { s.TransactionId, s.Weight }).ToQueryString());
    await db.Database.ExecuteSqlRawAsync("DROP INDEX IF EXISTS IX_Stock_Branch_Quantity ON dbo.BranchStock;DROP INDEX IF EXISTS IX_Transactions_Branch_Date ON dbo.CounterTransactions;DROP INDEX IF EXISTS IX_Shipment_Zone_Level ON dbo.Shipments;DROP INDEX IF EXISTS IX_MoneyOrder_Branch_Date ON dbo.MoneyOrders;");
    var queries = new Dictionary<string, string>
    {
        ["low-stock"] = "DECLARE @Branch uniqueidentifier=(SELECT Id FROM dbo.Branches WHERE Code='PERF-6'); SELECT ProductId,Quantity FROM dbo.BranchStock WHERE BranchId=@Branch AND Quantity<=3;",
        ["daily-sales"] = "DECLARE @Branch uniqueidentifier=(SELECT TOP(1)Id FROM dbo.Branches ORDER BY Code); SELECT Id,OccurredAt,Status FROM dbo.CounterTransactions WHERE BranchId=@Branch AND OccurredAt>=DATEADD(day,-30,SYSUTCDATETIME()) AND OccurredAt<SYSUTCDATETIME();",
        ["shipments"] = "DECLARE @Zone uniqueidentifier=(SELECT Id FROM dbo.ShippingZones WHERE Code='ZONE-1'); SELECT TransactionId,Weight FROM dbo.Shipments WHERE ZoneId=@Zone AND ServiceLevel=0;",
        ["money-orders"] = "DECLARE @Branch uniqueidentifier=(SELECT TOP(1)BranchId FROM dbo.MoneyOrders ORDER BY BranchId); SELECT Id,OrderAmount,IdentityVerificationId FROM dbo.MoneyOrders WHERE BranchId=@Branch AND OccurredAtUtc>=DATEADD(day,-30,SYSUTCDATETIME()) AND OccurredAtUtc<SYSUTCDATETIME();"
    };
    var results = new List<object>();
    foreach (var phase in new[]
    {
        "baseline",
        "indexed"
    }

    )
    {
        if (phase == "indexed")
            await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync("sql/performance/CandidateIndexes.sql"));
        var storage = await db.Database.SqlQuery<StorageRow>($"SELECT OBJECT_NAME(p.object_id) AS TableName,i.name AS IndexName,CAST(SUM(p.used_page_count)*8 AS bigint) AS UsedKB FROM sys.dm_db_partition_stats p INNER JOIN sys.indexes i ON i.object_id=p.object_id AND i.index_id=p.index_id WHERE OBJECT_NAME(p.object_id) IN ('BranchStock','CounterTransactions','Shipments','MoneyOrders') GROUP BY p.object_id,i.name").ToArrayAsync();
        await File.WriteAllTextAsync(Path.Combine(folder, phase + "-storage.json"), JsonSerializer.Serialize(storage, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var pair in queries)
            results.Add(await Capture(phase + "-" + pair.Key, pair.Value));
        results.Add(await Capture(phase + "-write", "BEGIN TRAN; UPDATE dbo.BranchStock SET Quantity=Quantity+1 WHERE BranchId=(SELECT TOP(1)Id FROM dbo.Branches ORDER BY Code); ROLLBACK;"));
    }

    results.Add(await Capture("non-sargable", "SELECT Id FROM dbo.CounterTransactions WHERE YEAR(OccurredAt)=2026;"));
    results.Add(await Capture("sargable", "SELECT Id FROM dbo.CounterTransactions WHERE OccurredAt>='20260101' AND OccurredAt<'20270101';"));
    // Deliberately stale statistics, isolated database only: compare recorded estimate with actual then update statistics.
    await db.Database.ExecuteSqlRawAsync("CREATE STATISTICS ST_Evidence_Quantity ON dbo.BranchStock(Quantity) WITH FULLSCAN, NORECOMPUTE; UPDATE dbo.BranchStock SET Quantity=0 WHERE Quantity>3;");
    results.Add(await Capture("stale-statistics", "SELECT ProductId FROM dbo.BranchStock WHERE Quantity=0 OPTION(RECOMPILE);"));
    await db.Database.ExecuteSqlRawAsync("UPDATE STATISTICS dbo.BranchStock WITH FULLSCAN;");
    results.Add(await Capture("fresh-statistics", "SELECT ProductId FROM dbo.BranchStock WHERE Quantity=0 OPTION(RECOMPILE);"));
    await File.WriteAllTextAsync(Path.Combine(folder, "measurements.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
    await using (var lockConnection = new SqlConnection(builder.ConnectionString))
    await using (var readConnection = new SqlConnection(builder.ConnectionString))
    {
        await lockConnection.OpenAsync();
        await readConnection.OpenAsync();
        await using var transaction = (SqlTransaction)await lockConnection.BeginTransactionAsync();
        await using var update = lockConnection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = "UPDATE dbo.BranchStock SET Quantity=Quantity WHERE Id=(SELECT TOP(1)Id FROM dbo.BranchStock ORDER BY Id)";
        await update.ExecuteNonQueryAsync();
        await using var read = readConnection.CreateCommand();
        read.CommandText = "SET TRANSACTION ISOLATION LEVEL READ COMMITTED;SET LOCK_TIMEOUT 1500;SELECT Quantity FROM dbo.BranchStock WHERE Id=(SELECT TOP(1)Id FROM dbo.BranchStock ORDER BY Id)";
        var watch = System.Diagnostics.Stopwatch.StartNew();
        string observed;
        try
        {
            await read.ExecuteScalarAsync();
            observed = "Read completed without blocking (inspect RCSI).";
        }
        catch (SqlException ex)when (ex.Number == 1222)
        {
            observed = "Read timed out waiting for the other session's stock lock (1222).";
        }

        var defaultElapsed = watch.ElapsedMilliseconds;
        read.CommandText = "SET LOCK_TIMEOUT 1500;SELECT Quantity FROM dbo.BranchStock WITH(READCOMMITTEDLOCK) WHERE Id=(SELECT TOP(1)Id FROM dbo.BranchStock ORDER BY Id)";
        watch.Restart();
        string lockingRead;
        try
        {
            await read.ExecuteScalarAsync();
            lockingRead = "Locking read unexpectedly completed.";
        }
        catch (SqlException ex)when (ex.Number == 1222)
        {
            lockingRead = "READCOMMITTEDLOCK read blocked and timed out with error 1222.";
        }

        await transaction.RollbackAsync();
        await File.WriteAllTextAsync(Path.Combine(folder, "blocking.txt"), $"Two independent SQL Server sessions, READ COMMITTED. Default read: {observed} Elapsed ms={defaultElapsed}. Locking read: {lockingRead} Elapsed ms={watch.ElapsedMilliseconds}. EF-created SQL databases use RCSI ON; the table hint deliberately requests shared locks for the blocking demonstration. SSMS companion scripts are in sql/evidence/.\n");
    }

    await using var connection = new SqlConnection(builder.ConnectionString);
    await connection.OpenAsync();
    await using var count = connection.CreateCommand();
    count.CommandText = "SELECT (SELECT COUNT(*) FROM Branches),(SELECT COUNT(*) FROM BranchStock),(SELECT COUNT(*) FROM CounterTransactions),(SELECT COUNT(*) FROM Shipments),(SELECT COUNT(*) FROM MoneyOrders),CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(100))";
    await using var reader = await count.ExecuteReaderAsync();
    await reader.ReadAsync();
    await File.WriteAllTextAsync(Path.Combine(folder, "environment.txt"), $"UTC run: {DateTime.UtcNow:O}\nBranches: {reader.GetInt32(0)}\nStock: {reader.GetInt32(1)}\nTransactions: {reader.GetInt32(2)}\nShipments: {reader.GetInt32(3)}\nMoneyOrders: {reader.GetInt32(4)}\nSQL Server: {reader.GetString(5)}\nWarm-cache single run; no production claims.\n");
    Console.WriteLine("Saved genuine plans and measurements to evidence/. Disposable database will be removed.");
}
finally
{
    await db.Database.EnsureDeletedAsync();
}

async Task<object> Capture(string name, string sql)
{
    await using var connection = new SqlConnection(builder.ConnectionString);
    var messages = new List<string>();
    connection.InfoMessage += (_, e) => messages.Add(e.Message);
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandTimeout = 120;
    command.CommandText = "SET STATISTICS IO ON;SET STATISTICS TIME ON;SET STATISTICS XML ON;";
    await command.ExecuteNonQueryAsync();
    command.CommandText = sql;
    await using var reader = await command.ExecuteReaderAsync();
    var plans = new List<string>();
    var rows = 0;
    do
    {
        while (await reader.ReadAsync())
        {
            if (reader.FieldCount == 1 && reader.GetName(0).Contains("Showplan", StringComparison.OrdinalIgnoreCase))
                plans.Add(reader.GetValue(0).ToString()!);
            else
                rows++;
        }
    }
    while (await reader.NextResultAsync());
    for (var i = 0; i < plans.Count; i++)
        await File.WriteAllTextAsync(Path.Combine(folder, $"plans/{name}-{i}.sqlplan"), plans[i]);
    await File.WriteAllTextAsync(Path.Combine(folder, name + ".txt"), string.Join("\n", messages));
    var text = string.Join("\n", messages);
    var reads = Regex.Matches(text, "logical reads (\\d+)").Sum(m => int.Parse(m.Groups[1].Value));
    var cpu = Regex.Matches(text, "CPU time = (\\d+) ms").Select(m => int.Parse(m.Groups[1].Value)).ToArray();
    var shapes = new List<string>();
    foreach (var plan in plans)
    {
        var xml = XDocument.Parse(plan);
        shapes.AddRange(xml.Descendants().Where(x => x.Name.LocalName == "RelOp").Select(x => x.Attribute("PhysicalOp")?.Value ?? ""));
    }

    return new
    {
        Name = name,
        LogicalReads = reads,
        CpuMs = cpu.LastOrDefault(),
        Rows = rows,
        PlanFiles = plans.Count,
        Operators = shapes.Distinct().ToArray()
    };
}

public sealed class StorageRow
{
    public string TableName { get; set; } = "";
    public string IndexName { get; set; } = "";
    public long UsedKB { get; set; }
}
