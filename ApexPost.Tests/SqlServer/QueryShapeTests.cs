using System.Data.Common;
using System.Text.Json;
using ApexPost.Domain.Entities;
using ApexPost.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit.Abstractions;

namespace ApexPost.Tests.SqlServer;

[Collection("SqlServer"), Trait("Category", "SqlServer")]
public sealed class QueryShapeTests(SqlServerFixture fixture, ITestOutputHelper output)
{
    private sealed class Counter : DbCommandInterceptor
    {
        public int Commands { get; set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData data, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands++;
            return ValueTask.FromResult(result);
        }
    }

    [SqlServerFact]
    public async Task Single_and_split_load_identical_aggregates_with_bounded_commands()
    {
        var count = new Counter();
        await using var db = new ApexPostDbContext(new DbContextOptionsBuilder<ApexPostDbContext>()
            .UseSqlServer(fixture.Connection, sql => sql.EnableRetryOnFailure()).AddInterceptors(count).Options);
        var ids = await db.Set<Transaction>().OrderBy(t => t.Id).Select(t => t.Id).Take(20).ToArrayAsync();
        var query = db.Set<Transaction>().AsNoTracking().Where(t => ids.Contains(t.Id))
            .OrderBy(t => t.Id).Include(t => t.Lines);
        count.Commands = 0;
        var single = await query.AsSingleQuery().ToArrayAsync();
        var singleCommands = count.Commands;
        count.Commands = 0;
        var split = await query.AsSplitQuery().ToArrayAsync();
        var splitCommands = count.Commands;
        static object[] Shape(Transaction[] rows) => rows.Select(t => (object)new
        {
            t.Id, t.Status, t.Total,
            Lines = t.Lines.OrderBy(l => l.Id).Select(l => new { l.Id, l.Type, l.Code, l.Total }).ToArray()
        }).ToArray();
        Assert.Equal(JsonSerializer.Serialize(Shape(single)), JsonSerializer.Serialize(Shape(split)));
        Assert.Equal(1, singleCommands);
        Assert.Equal(2, splitCommands);
        Assert.Empty(db.ChangeTracker.Entries());
        var physicalSingleRows = single.Sum(t => Math.Max(1, t.Lines.Count));
        output.WriteLine($"Transactions={single.Length}; lines={single.Sum(t => t.Lines.Count)}; single commands={singleCommands}; split commands={splitCommands}; single joined rows={physicalSingleRows}");
        // One collection duplicates headers; it does not multiply sibling collections.
        // Keep production's one-command aggregate load unless measured workload evidence favors splitting.
    }
}
