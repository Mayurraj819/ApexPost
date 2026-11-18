using ApexPost.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.IntegrationTests.Infrastructure;

// Each test owns a new database. Never migrate or delete the configured base database.
internal sealed class SqlTestDatabase : IAsyncDisposable
{
    private const string Prefix = "ApexPost_Test_Week5_";
    private readonly string _databaseName = Prefix + Guid.NewGuid().ToString("N");
    public string ConnectionString { get; }

    private SqlTestDatabase(string baseConnection)
    {
        var builder = new SqlConnectionStringBuilder(baseConnection);
        if (!string.Equals(builder.InitialCatalog, "master", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(builder.AttachDBFilename))
        {
            throw new InvalidOperationException(
                "APEXPOST_TEST_SQLSERVER must target master without AttachDBFilename. Use a private test SQL Server with database-create rights.");
        }

        builder.InitialCatalog = _databaseName;
        builder.Pooling = false;
        builder.ConnectTimeout = 30;
        ConnectionString = builder.ConnectionString;
    }

    public static async Task<SqlTestDatabase> CreateAsync()
    {
        var baseConnection = Environment.GetEnvironmentVariable("APEXPOST_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new InvalidOperationException(
                    "These tests require SQL Server. Windows defaults to installed (localdb)\\MSSQLLocalDB. On other systems set APEXPOST_TEST_SQLSERVER to a private test server connection with Database=master and database-create rights. No test is silently skipped.");
            }

            baseConnection = @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True";
        }

        var database = new SqlTestDatabase(baseConnection);
        try
        {
            await using var db = database.CreateContext();
            await db.Database.MigrateAsync();
            return database;
        }
        catch
        {
            // Preserve the setup failure if cleanup is also unavailable (for example, server offline).
            try { await database.DisposeAsync(); }
            catch { /* No shared/application database can be selected by this instance. */ }
            throw;
        }
    }

    public ApexPostDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApexPostDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.EnableRetryOnFailure(
                maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(1), errorNumbersToAdd: null))
            .Options);

    public async ValueTask DisposeAsync()
    {
        var actual = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
        if (actual != _databaseName || !actual.StartsWith(Prefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing to delete a database not owned by this test.");

        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }
}
