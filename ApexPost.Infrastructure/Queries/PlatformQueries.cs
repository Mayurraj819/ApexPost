using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Contracts;
using ApexPost.Domain.Entities;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;
using ApexPost.Infrastructure.Persistence;
using ApexPost.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Infrastructure.Queries;
public sealed class PlatformQueries(ApexPostDbContext db) : IPlatformQueries
{
    private IQueryable<ProductRow> Products => db.Set<Product>().AsNoTracking().Select(p => new ProductRow(p.Id, p.Sku, p.Name, p.UnitPrice, p.Category, p.Weight));

    public async Task<IReadOnlyList<ProductRow>> ProductsAsync(string? search, CancellationToken ct)
    {
        var query = db.Set<Product>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Sku == new Sku(search));
        // SKU exact matching uses its converter; broad substring search stays on mapped Name.
        return await query.OrderBy(p => p.Name).Select(p => new ProductRow(p.Id, p.Sku, p.Name, p.UnitPrice, p.Category, p.Weight)).ToArrayAsync(ct);
    }

    public Task<ProductRow?> ProductAsync(Guid id, CancellationToken ct) => db.Set<Product>().AsNoTracking().Where(p => p.Id == id).Select(p => new ProductRow(p.Id, p.Sku, p.Name, p.UnitPrice, p.Category, p.Weight)).SingleOrDefaultAsync(ct);
    public async Task<IReadOnlyList<StockRow>> StockAsync(Guid branchId, CancellationToken ct)
    {
        var rows = await (
            from p in db.Set<Product>().AsNoTracking()join s in db.Set<BranchStock>().AsNoTracking().Where(s => s.BranchId == branchId)on p.Id equals s.ProductId into stocks
            from s in stocks.DefaultIfEmpty()select new
            {
                p.Id,
                p.Sku,
                p.Name,
                Quantity = s == null ? 0 : EF.Property<int>(s, "_quantity"),
                Exists = s != null
            }

        ).ToArrayAsync(ct);
        return rows.Select(x => new StockRow(x.Id, x.Sku.Value, x.Name, x.Quantity, x.Exists)).ToArray();
    }

    public async Task<IReadOnlyList<CounterRow>> CountersAsync(Guid branchId, CancellationToken ct) => await db.Set<Counter>().AsNoTracking().Where(x => x.BranchId == branchId).Select(x => new CounterRow(x.Id, x.BranchId, x.Number, x.Name)).ToArrayAsync(ct);
    private IQueryable<Transaction> TransactionQuery => db.Set<Transaction>().AsNoTracking();

    public async Task<IReadOnlyList<TransactionRow>> TransactionsAsync(Guid branchId, CancellationToken ct)
    {
        // Bounded list projection; aggregate detail lines only on Details, not N+1 per row.
        return await TransactionQuery.Where(t => t.BranchId == branchId).OrderByDescending(t => t.OccurredAt).Take(200).Select(t => new TransactionRow(t.Id, t.BranchId, t.Status, t.StatusReason, t.OccurredAt, Array.Empty<LineRow>()) { SummaryTotal = new Money(t.Lines.Sum(l => l.UnitPrice.Amount * EF.Property<int>(l, "_quantity")), "PHP") }).ToArrayAsync(ct);
    }

    public async Task<TransactionRow?> TransactionAsync(Guid id, CancellationToken ct)
    {
        var header = await TransactionQuery.Where(t => t.Id == id).Select(t => new { t.Id, t.BranchId, t.Status, t.StatusReason, t.OccurredAt }).SingleOrDefaultAsync(ct);
        if (header is null)
            return null;
        var lines = await db.Set<TransactionLine>().AsNoTracking().Where(l => EF.Property<Guid>(l, "TransactionId") == id).Select(l => new LineRow(l.Code, l.Type, new Quantity(EF.Property<int>(l, "_quantity")), l.UnitPrice, l.ShippingLevel)).ToArrayAsync(ct);
        return new(header.Id, header.BranchId, header.Status, header.StatusReason, header.OccurredAt, lines);
    }

    public async Task<DashboardData> DashboardAsync(Guid branchId, CancellationToken ct)
    {
        var branches = await db.Set<Branch>().AsNoTracking().Where(b => b.IsActive).Select(b => new BranchRow(b.Id, b.Name)).ToArrayAsync(ct);
        var products = await ProductsAsync(null, ct);
        var stock = await StockAsync(branchId, ct);
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var report = await ReportsAsync(branchId, from, to, ct);
        var total = report.Sales.Sum(x => x.Retail + x.Services);
        var counts = await db.Set<Transaction>().AsNoTracking().Where(t => t.BranchId == branchId && t.OccurredAt >= @from && t.OccurredAt < to).GroupBy(t => t.BranchId).Select(g => new { Count = g.Count(), Paid = g.Count(t => t.Status == TransactionStatus.Paid), Voided = g.Count(t => t.Status == TransactionStatus.Voided), Refunded = g.Count(t => t.Status == TransactionStatus.Refunded) }).SingleOrDefaultAsync(ct);
        return new(branches, products, stock.Where(x => x.Quantity <= 3).Select(x => $"Low stock: {x.Name} ({x.Quantity} remaining)").ToArray(), new TransactionReport(branchId, from, to, counts?.Count ?? 0, counts?.Paid ?? 0, counts?.Voided ?? 0, counts?.Refunded ?? 0, new Money(total)));
    }

    public async Task<FullReportsViewModel> ReportsAsync(Guid? branchId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        if (from > to || to - from > TimeSpan.FromDays(366))
            throw new ArgumentException("Choose a report range of at most 366 days.");
        var paid = db.Set<Transaction>().AsNoTracking().Where(t => (branchId == null || t.BranchId == branchId) && t.Status == TransactionStatus.Paid && t.OccurredAt >= from && t.OccurredAt < to);
        var flat =
            from t in paid
            join l in db.Set<TransactionLine>().AsNoTracking()on t.Id equals EF.Property<Guid>(l, "TransactionId")select new
            {
                t.BranchId,
                t.OccurredAt,
                l.Type,
                l.UnitPrice.Currency,
                Amount = l.UnitPrice.Amount * EF.Property<int>(l, "_quantity")
            };
        var data = await flat.GroupBy(x => new { x.BranchId, Day = x.OccurredAt.Date, x.Currency }).Select(g => new { g.Key.BranchId, g.Key.Day, g.Key.Currency, Retail = g.Sum(x => x.Type == TransactionLineType.RetailProduct ? x.Amount : 0), Services = g.Sum(x => x.Type != TransactionLineType.RetailProduct ? x.Amount : 0) }).ToArrayAsync(ct);
        var sales = data.Select(x => new SalesSplitRow(x.BranchId, DateOnly.FromDateTime(x.Day), x.Retail, x.Services, x.Currency)).ToArray();
        var low = await (
            from s in db.Set<BranchStock>().AsNoTracking()join p in db.Set<Product>().AsNoTracking()on s.ProductId equals p.Id
            where (branchId == null || s.BranchId == branchId) && EF.Property<int>(s, "_quantity") <= 3
            select new StockWarningRow(s.BranchId, p.Id, p.Name, EF.Property<int>(s, "_quantity"))).ToArrayAsync(ct);
        var shipments =
            from s in db.Set<ShipmentRecord>().AsNoTracking()join t in paid on s.TransactionId equals t.Id
            join z in db.Set<ShippingZoneRecord>().AsNoTracking()on s.ZoneId equals z.Id
            select new
            {
                s.ZoneId,
                z.Code,
                s.ServiceLevel,
                t.BranchId,
                s.Charge.Amount
            };
        var shippingData = await shipments.GroupBy(x => new { x.BranchId, x.Code, x.ServiceLevel }).Select(g => new { g.Key.BranchId, g.Key.Code, g.Key.ServiceLevel, Count = g.Count(), Revenue = g.Sum(x => x.Amount) }).ToArrayAsync(ct);
        var shipping = shippingData.Select(x => new ShippingVolumeRow(x.BranchId, x.ServiceLevel.ToString(), x.Count, x.Revenue, x.Code)).ToArray();
        var orders = await (
            from m in db.Set<MoneyOrderRecord>().AsNoTracking()join v in db.Set<IdentityVerificationRecord>().AsNoTracking()on m.IdentityVerificationId equals v.Id
            where (branchId == null || m.BranchId == branchId) && m.OccurredAtUtc >= @from && m.OccurredAtUtc < to
            group new
            {
                m,
                v
            }

            by m.BranchId into g
                select new MoneyOrderVolumeRow(g.Key, g.Count(), g.Sum(x => x.m.Amount.Amount), g.Count(x => x.v.Fingerprint != ""), g.Count(x => x.v.IsVerified))).ToArrayAsync(ct);
        return new(sales, low, shipping, orders, from, to);
    }

    public async Task<OperationalAlertsViewModel> AlertsAsync(Guid? branchId, CancellationToken ct)
    {
        var report = await ReportsAsync(branchId, DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, ct);
        var corrections = await TransactionQuery.Where(t => (branchId == null || t.BranchId == branchId) && (t.Status == TransactionStatus.Voided || t.Status == TransactionStatus.Refunded)).OrderByDescending(t => t.OccurredAt).Take(100).Select(t => new CorrectionAlert(t.Id, t.BranchId, t.Status.ToString(), t.StatusReason, t.OccurredAt)).ToArrayAsync(ct);
        var high = await db.Set<MoneyOrderRecord>().AsNoTracking().Where(m => (branchId == null || m.BranchId == branchId) && m.Amount.Amount >= 10000).Select(m => new HighValueMoneyOrderAlert(m.Id, m.BranchId, m.Amount.Amount, m.Amount.Currency, m.OccurredAtUtc)).ToArrayAsync(ct);
        return new(report.LowStock.Select(x => new LowStockAlert(x.BranchId, x.ProductId, x.Product, x.Quantity)).ToArray(), high, corrections, 3, 10000);
    }

    public async Task<string?> BranchCodeAsync(Guid branchId, CancellationToken ct) => (await db.Set<Branch>().AsNoTracking().Where(b => b.Id == branchId).Select(b => b.Code).SingleOrDefaultAsync(ct))?.Value;
    public async Task<IReadOnlyList<ZoneRow>> ZonesAsync(CancellationToken ct) => await db.Set<ShippingZoneRecord>().AsNoTracking().Select(z => new ZoneRow(z.Id, z.Code, z.Name)).ToArrayAsync(ct);
}
