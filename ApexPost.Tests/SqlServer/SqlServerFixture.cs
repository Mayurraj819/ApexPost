using ApexPost.Infrastructure.Persistence;
using ApexPost.Infrastructure.Seeding;
using ApexPost.IntegrationTests.Infrastructure;

namespace ApexPost.Tests.SqlServer;

// Retained attribute name preserves existing test/category discovery. No silent SQL skips.
public sealed class SqlServerFactAttribute : FactAttribute;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private SqlTestDatabase? _database;
    public string Connection => Database.ConnectionString;
    private SqlTestDatabase Database => _database
        ?? throw new InvalidOperationException("The per-test SQL database has not been initialized.");

    public ApexPostDbContext Create() => Database.CreateContext();

    public async Task InitializeAsync()
    {
        _database = await SqlTestDatabase.CreateAsync();
        try
        {
            await using var db = Create();
            await new ApexPostSeeder(db).SeedAsync();
        }
        catch
        {
            try { await DisposeAsync(); }
            catch { /* Preserve the seed/setup failure if cleanup also fails. */ }
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (_database is { } database)
        {
            _database = null;
            await database.DisposeAsync();
        }
    }
}

// xUnit creates a fresh test-class instance per case. The inherited lifetime therefore
// creates/seeds/drops one database per case, including when its assertions fail.
public abstract class SqlServerTestBase : IAsyncLifetime
{
    protected readonly SqlServerFixture fixture = new();
    public Task InitializeAsync() => fixture.InitializeAsync();
    public Task DisposeAsync() => fixture.DisposeAsync();
}

// Serialize legacy SQL cases for bounded LocalDB resource usage, not shared data.
[CollectionDefinition("SqlServer", DisableParallelization = true)]
public sealed class SqlServerCollection;
