using System.Data.Common;
using ApexPost.Infrastructure.Persistence;
using ApexPost.Infrastructure.Queries;
using ApexPost.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ApexPost.Tests.SqlServer;
[Collection("SqlServer"), Trait("Category", "SqlServer")]
public sealed class QueryCountTests(SqlServerFixture fixture)
{
    private sealed class Counter : DbCommandInterceptor
    {
        public int Reads { get; set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData data, InterceptionResult<DbDataReader> result, CancellationToken ct = default)
        {
            Reads++;
            return ValueTask.FromResult(result);
        }
    }

    [SqlServerFact]
    public async Task Reports_execute_four_queries_for_one_or_all_branches()
    {
        var count = new Counter();
        await using var db = new ApexPostDbContext(new DbContextOptionsBuilder<ApexPostDbContext>().UseSqlServer(fixture.Connection).AddInterceptors(count).Options);
        var branch = await db.Set<Branch>().Select(x => x.Id).FirstAsync();
        var queries = new PlatformQueries(db);
        count.Reads = 0;
        await queries.ReportsAsync(branch, DateTimeOffset.UtcNow.AddDays(-60), DateTimeOffset.UtcNow, CancellationToken.None);
        Assert.Equal(4, count.Reads);
        count.Reads = 0;
        await queries.ReportsAsync(null, DateTimeOffset.UtcNow.AddDays(-60), DateTimeOffset.UtcNow, CancellationToken.None);
        Assert.Equal(4, count.Reads);
        Assert.Empty(db.ChangeTracker.Entries());
    }
}
