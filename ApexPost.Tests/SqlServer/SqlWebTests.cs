using System.Net;
using System.Text.RegularExpressions;
using ApexPost.Domain.Entities;
using ApexPost.Domain.Enums;
using ApexPost.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using ApexPost.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApexPost.Tests.SqlServer;
[Collection("SqlServer"), Trait("Category", "WebSql")]
public sealed class SqlWebTests(SqlServerFixture fixture)
{
    private sealed class Factory(string connection, Guid branch, Guid staff, string role, SaveChangesInterceptor? interceptor = null, bool failAudit = false) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<ApexPostDbContext>(o =>
                {
                    o.UseSqlServer(connection);
                    if (interceptor is not null)
                        o.AddInterceptors(interceptor);
                });
                if (failAudit)
                    services.AddScoped<ApexPost.Application.Abstractions.Persistence.IAuditStore, FailingAudit>();
            });
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:ApexPost"] = connection, ["ApexPost:DemoAccounts:Accounts:0:Username"] = "sql-test", ["ApexPost:DemoAccounts:Accounts:0:DisplayName"] = "SQL test clerk", ["ApexPost:DemoAccounts:Accounts:0:Password"] = "Fictional-Test-Password!", ["ApexPost:DemoAccounts:Accounts:0:BranchId"] = branch.ToString(), ["ApexPost:DemoAccounts:Accounts:0:StaffId"] = staff.ToString(), ["ApexPost:DemoAccounts:Accounts:0:Role"] = role, ["ApexPost:Security:LoginRateLimitPermitLimit"] = "100" }));
        }
    }

    private static string Token(string html) => Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
    private static async Task Login(HttpClient client)
    {
        var token = Token(await client.GetStringAsync("/Account/Login"));
        Assert.NotEmpty(token);
        using var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", token }, { "Username", "sql-test" }, { "Password", "Fictional-Test-Password!" } }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [SqlServerFact]
    public async Task Sale_persists_and_reports_match_sql_totals()
    {
        await using var db = fixture.Create();
        var clerk = await db.Set<Staff>().Where(s => s.IsActive).FirstAsync(x => x.Role == StaffRole.CounterClerk);
        // Test fixture opening hours are explicitly UTC and made all-day to avoid clock-dependent UI tests.
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Branches SET OpeningHours_OpensAt='00:00:00',OpeningHours_ClosesAt='23:59:59' WHERE Id={clerk.BranchId}");
        var counter = await db.Set<Counter>().FirstAsync(x => x.BranchId == clerk.BranchId);
        var product = await db.Set<Product>().Where(p => p.Name.StartsWith("Demo product")).FirstAsync();
        using var factory = new Factory(fixture.Connection, clerk.BranchId, clerk.Id, "CounterClerk", failAudit: true);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        await Login(client);
        var token = Token(await client.GetStringAsync("/Transactions/New"));
        using var start = await client.PostAsync("/Transactions/Start", new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", token }, { "CounterId", counter.Id.ToString() }, { "Status", "Paid" }, { "Id", Guid.NewGuid().ToString() }, { "CreatedBy", "attacker" } }));
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        var location = start.Headers.Location!.ToString();
        var id = Guid.Parse(location.Split('/').Last());
        token = Token(await client.GetStringAsync(location));
        using var add = await client.PostAsync($"/Transactions/AddProduct/{id}", new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", token }, { "ProductId", product.Id.ToString() }, { "Quantity", "1" } }));
        Assert.Equal(HttpStatusCode.Redirect, add.StatusCode);
        token = Token(await client.GetStringAsync(location));
        using var pay = await client.PostAsync($"/Transactions/Pay/{id}", new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", token } }));
        Assert.Equal(HttpStatusCode.Redirect, pay.StatusCode);
        await using var verify = fixture.Create();
        var stored = await verify.Set<Transaction>().Include(t => t.Lines).SingleAsync(t => t.Id == id);
        Assert.Equal(TransactionStatus.Paid, stored.Status);
        Assert.Single(stored.Lines);
        Assert.True(await verify.Set<Payment>().AnyAsync(p => p.TransactionId == id));
        Assert.NotEqual("attacker", verify.Entry(stored).Property<string>("CreatedBy").CurrentValue);
        using var report = await client.GetAsync("/Reports");
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.Contains("Daily retail", await report.Content.ReadAsStringAsync());
    }

    [SqlServerFact]
    public async Task Clerk_cannot_void_and_not_found_returns_problem_details()
    {
        await using var db = fixture.Create();
        var clerk = await db.Set<Staff>().Where(s => s.IsActive).FirstAsync(x => x.Role == StaffRole.CounterClerk);
        using var factory = new Factory(fixture.Connection, clerk.BranchId, clerk.Id, "CounterClerk");
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        await Login(client);
        var token = Token(await client.GetStringAsync("/Transactions/New"));
        using var denied = await client.PostAsync($"/Transactions/Void/{Guid.NewGuid()}", new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", token }, { "Reason", "Test" } }));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var missing = await client.GetAsync($"/Catalog/Details/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType!.MediaType);
    }

    private sealed class CheckoutBarrier : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _count;
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<Payment>().Any(e => e.State == EntityState.Added))
            {
                if (Interlocked.Increment(ref _count) == 2)
                    _ready.TrySetResult();
                await _ready.Task.WaitAsync(TimeSpan.FromSeconds(15), ct);
            }

            return result;
        }
    }

    [SqlServerFact]
    public async Task Two_checkout_requests_for_last_unit_return_one_success_and_one_409()
    {
        Guid[] saleIds;
        Guid branchId;
        Guid staffId;
        await using (var setup = fixture.Create())
        {
            var clerk = await setup.Set<Staff>().Where(s => s.IsActive).FirstAsync(x => x.Role == StaffRole.CounterClerk);
            branchId = clerk.BranchId;
            staffId = clerk.Id;
            var branch = await setup.Set<Branch>().SingleAsync(b => b.Id == branchId);
            var counter = await setup.Set<Counter>().FirstAsync(c => c.BranchId == branchId);
            var product = await setup.Set<Product>().Where(p => p.Name.StartsWith("Demo product")).FirstAsync();
            await setup.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.BranchStock SET Quantity=1 WHERE BranchId={branchId} AND ProductId={product.Id}");
            var sales = Enumerable.Range(0, 2).Select(_ =>
            {
                var sale = Transaction.Start(branch, counter, clerk, new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
                sale.AddProduct(product, new Quantity(1));
                return sale;
            }).ToArray();
            setup.AddRange(sales);
            await setup.SaveChangesAsync();
            saleIds = sales.Select(t => t.Id).ToArray();
        }

        using var factory = new Factory(fixture.Connection, branchId, staffId, "CounterClerk", new CheckoutBarrier());
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        await Login(client);
        var token = Token(await client.GetStringAsync($"/Transactions/Details/{saleIds[0]}"));
        async Task<HttpResponseMessage> Pay(Guid id) => await client.PostAsync($"/Transactions/Pay/{id}", new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", token } }));
        var responses = await Task.WhenAll(saleIds.Select(Pay));
        try
        {
            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Redirect);
            var conflict = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal("application/problem+json", conflict.Content.Headers.ContentType!.MediaType);
            await using var verify = fixture.Create();
            Assert.Equal(1, await verify.Set<Payment>().CountAsync(p => saleIds.Contains(p.TransactionId)));
        }
        finally
        {
            foreach (var response in responses)
                response.Dispose();
        }
    }

    [SqlServerFact]
    public async Task Manager_can_void_a_draft_and_reports_have_expected_totals()
    {
        await using var db = fixture.Create();
        var manager = await db.Set<Staff>().Where(s => s.IsActive).FirstAsync(s => s.Role == StaffRole.BranchManager);
        var branch = await db.Set<Branch>().SingleAsync(b => b.Id == manager.BranchId);
        var counter = await db.Set<Counter>().FirstAsync(c => c.BranchId == branch.Id);
        var sale = Transaction.Start(branch, counter, manager, new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
        db.Add(sale);
        await db.SaveChangesAsync();
        using var factory = new Factory(fixture.Connection, branch.Id, manager.Id, "BranchManager");
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        await Login(client);
        var token = Token(await client.GetStringAsync($"/Transactions/Details/{sale.Id}"));
        using var response = await client.PostAsync($"/Transactions/Void/{sale.Id}", new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", token }, { "Reason", "Duplicate draft" } }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var verify = fixture.Create();
        Assert.Equal(TransactionStatus.Voided, (await verify.Set<Transaction>().SingleAsync(t => t.Id == sale.Id)).Status);
        var start = DateTimeOffset.UtcNow.AddDays(-60);
        var end = DateTimeOffset.UtcNow;
        var report = await new ApexPost.Infrastructure.Queries.PlatformQueries(verify).ReportsAsync(branch.Id, start, end, CancellationToken.None);
        var expected = await (
            from t in verify.Set<Transaction>()join l in verify.Set<TransactionLine>()on t.Id equals EF.Property<Guid>(l, "TransactionId")
            where t.BranchId == branch.Id && t.Status == TransactionStatus.Paid && t.OccurredAt >= start && t.OccurredAt < end
            select l.UnitPrice.Amount * EF.Property<int>(l, "_quantity")).SumAsync();
        Assert.Equal(expected, report.Sales.Sum(x => x.Retail + x.Services));
        // Final response auditing is asynchronous but bounded; it must record redirects as 302, not pre-result 200.
        var audited = false;
        for (var attempt = 0; attempt < 20 && !audited; attempt++)
        {
            audited = await verify.Set<ApexPost.Infrastructure.Persistence.Entities.AuditRecord>().AsNoTracking().AnyAsync(a => a.Controller == "Transactions" && a.Action == "Void" && a.StatusCode == 302 && a.SafeRoutes.Contains(sale.Id.ToString()));
            if (!audited)
                await Task.Delay(50);
        }

        Assert.True(audited);
    }

    private sealed class FailingAudit : ApexPost.Application.Abstractions.Persistence.IAuditStore
    {
        public Task AppendAsync(object value, CancellationToken cancellationToken = default) => throw new IOException("Test audit failure");
        public Task<IReadOnlyList<object>> ReadAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<object>>([]);
    }

    [SqlServerFact]
    public async Task Product_and_stock_forms_preserve_validation_and_history()
    {
        await using var db = fixture.Create();
        var manager = await db.Set<Staff>().Where(s => s.IsActive).FirstAsync(s => s.Role == StaffRole.BranchManager);
        using var factory = new Factory(fixture.Connection, manager.BranchId, manager.Id, "BranchManager");
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        await Login(client);
        var token = Token(await client.GetStringAsync("/Catalog/Create"));
        var sku = "CRUD-" + Guid.NewGuid().ToString("N");
        var malicious = Guid.NewGuid();
        using var create = await client.PostAsync("/Catalog/Create", new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", token }, { "Sku", sku }, { "Name", "CRUD fictional product" }, { "Price", "12.50" }, { "Category", ((int)ProductCategory.OfficeSupplies).ToString() }, { "Id", malicious.ToString() }, { "Status", "Paid" }, { "CreatedBy", "attacker" } }));
        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);
        var product = await db.Set<Product>().SingleAsync(p => p.Sku == new Sku(sku));
        Assert.NotEqual(malicious, product.Id);
        token = Token(await client.GetStringAsync("/Stock"));
        async Task<HttpResponseMessage> Set(int quantity) => await client.PostAsync("/Stock/Set", new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", token }, { "ProductId", product.Id.ToString() }, { "Quantity", quantity.ToString() } }));
        using (var set = await Set(5))
            Assert.Equal(HttpStatusCode.Redirect, set.StatusCode);
        using (var invalid = await Set(-1))
        {
            Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
            Assert.Contains("text/html", invalid.Content.Headers.ContentType!.MediaType);
        }

        using (var clear = await Set(0))
            Assert.Equal(HttpStatusCode.Redirect, clear.StatusCode);
        using (var remove = await client.PostAsync($"/Stock/Remove/{product.Id}", new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", token } })))
            Assert.Equal(HttpStatusCode.Redirect, remove.StatusCode);
        using (var delete = await client.PostAsync("/Catalog/Delete", new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", token }, { "Id", product.Id.ToString() } })))
            Assert.Equal(HttpStatusCode.Redirect, delete.StatusCode);
        await using var verify = fixture.Create();
        Assert.True(await verify.Set<Product>().AnyAsync(p => p.Id == product.Id));
        Assert.False(await verify.Set<BranchStock>().AnyAsync(s => s.BranchId == manager.BranchId && s.ProductId == product.Id));
        Assert.Equal(2, await verify.Set<ApexPost.Infrastructure.Persistence.Entities.StockAdjustmentRecord>().CountAsync(h => h.ProductId == product.Id));
    }
}
