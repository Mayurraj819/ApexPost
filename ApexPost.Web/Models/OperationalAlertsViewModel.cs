namespace ApexPost.Web.Models;

public sealed record LowStockAlert(Guid BranchId, Guid ProductId, string ProductName, int Remaining);
public sealed record HighValueMoneyOrderAlert(Guid Reference, Guid BranchId, decimal Amount, string Currency, DateTimeOffset CreatedAt);
public sealed record CorrectionAlert(Guid TransactionId, Guid BranchId, string Status, string? Reason, DateTimeOffset OccurredAt);

public sealed record OperationalAlertsViewModel(
    IReadOnlyList<LowStockAlert> LowStock,
    IReadOnlyList<HighValueMoneyOrderAlert> HighValueMoneyOrders,
    IReadOnlyList<CorrectionAlert> Corrections,
    int LowStockThreshold,
    decimal HighValueThreshold);
