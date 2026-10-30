namespace ApexPost.Web.Models;

public sealed record SalesSplitRow(Guid BranchId, DateOnly Day, decimal Retail, decimal Services, string Currency);
public sealed record StockWarningRow(Guid BranchId, Guid ProductId, string Product, int Quantity);
public sealed record ShippingVolumeRow(Guid BranchId, string ServiceLevel, int Count, decimal Revenue);
public sealed record MoneyOrderVolumeRow(Guid BranchId, int Count, decimal Total, int FingerprintsRecorded);

public sealed record FullReportsViewModel(
    IReadOnlyList<SalesSplitRow> Sales,
    IReadOnlyList<StockWarningRow> LowStock,
    IReadOnlyList<ShippingVolumeRow> Shipping,
    IReadOnlyList<MoneyOrderVolumeRow> MoneyOrders,
    DateTimeOffset From,
    DateTimeOffset To);
