using ApexPost.Infrastructure.Persistence;
using ApexPost.Infrastructure.Seeding;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Tests.SqlServer;
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("APEXPOST_TEST_SQLSERVER")))
            Skip = "Set APEXPOST_TEST_SQLSERVER to a SQL Server connection with database-create rights (Initial Catalog=master).";
    }
}

public sealed class SqlServerFixture : IAsyncLifetime
{
    public string Connection { get; private set; } = "";

    public ApexPostDbContext Create() => new(new DbContextOptionsBuilder<ApexPostDbContext>().UseSqlServer(Connection, sql => sql.EnableRetryOnFailure()).Options);
    public async Task InitializeAsync()
    {
        var value = Environment.GetEnvironmentVariable("APEXPOST_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(value))
            return;
        var builder = new SqlConnectionStringBuilder(value);
        if (!string.IsNullOrEmpty(builder.InitialCatalog) && !builder.InitialCatalog.Equals("master", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Test base connection must target master, never ApexPostDb or a shared database.");
        builder.InitialCatalog = "ApexPost_Test_" + Guid.NewGuid().ToString("N");
        Connection = builder.ConnectionString;
        await using var db = Create();
        await db.Database.MigrateAsync();
        await new ApexPostSeeder(db).SeedAsync();
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrEmpty(Connection))
            return;
        if (!new SqlConnectionStringBuilder(Connection).InitialCatalog.StartsWith("ApexPost_Test_", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing to delete non-test database.");
        await using var db = Create();
        await db.Database.EnsureDeletedAsync();
    }
}

[CollectionDefinition("SqlServer", DisableParallelization = true)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>;
