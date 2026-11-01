using ApexPost.Application.Contracts;

namespace ApexPost.Application.Abstractions.Services;
public interface ISqlOperations
{
    Task<IReadOnlyList<DailySalesProcedureRow>> DailySalesAsync(string branchCode, DateOnly day, CancellationToken ct);
    Task<IReadOnlyList<LowStockProcedureRow>> LowStockAsync(string? branchCode, int threshold, CancellationToken ct);
    Task AdjustStockAsync(string branchCode, string sku, int delta, string reason, string changedBy, CancellationToken ct);
    Task<int> RevisePricesAsync(decimal factor, string changedBy, CancellationToken ct);
}
