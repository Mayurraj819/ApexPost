using ApexPost.Application.Contracts;

namespace ApexPost.Application.Abstractions.Services;
public interface IPlatformQueries
{
    Task<IReadOnlyList<ProductRow>> ProductsAsync(string? search, CancellationToken ct);
    Task<ProductRow?> ProductAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<StockRow>> StockAsync(Guid branchId, CancellationToken ct);
    Task<IReadOnlyList<CounterRow>> CountersAsync(Guid branchId, CancellationToken ct);
    Task<IReadOnlyList<TransactionRow>> TransactionsAsync(Guid branchId, CancellationToken ct);
    Task<TransactionRow?> TransactionAsync(Guid id, CancellationToken ct);
    Task<DashboardData> DashboardAsync(Guid branchId, CancellationToken ct);
    Task<FullReportsViewModel> ReportsAsync(Guid? branchId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    Task<OperationalAlertsViewModel> AlertsAsync(Guid? branchId, CancellationToken ct);
    Task<string?> BranchCodeAsync(Guid branchId, CancellationToken ct);
    Task<IReadOnlyList<ZoneRow>> ZonesAsync(CancellationToken ct);
}
