using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Application.Contracts;
public sealed record ProductRow(Guid Id, Sku Sku, string Name, Money UnitPrice, ProductCategory Category, Weight? Weight);
public sealed record BranchRow(Guid Id, string Name);
public sealed record CounterRow(Guid Id, Guid BranchId, int Number, string Name);
public sealed record StockRow(Guid ProductId, string Sku, string Name, int Quantity, bool Exists = true);
public sealed record LineRow(string Code, TransactionLineType Type, Quantity Quantity, Money UnitPrice, ShippingServiceLevel? ShippingLevel)
{
    public Money Total => new(Quantity.Value * UnitPrice.Amount, UnitPrice.Currency);
}

public sealed record TransactionRow(Guid Id, Guid BranchId, TransactionStatus Status, string? StatusReason, DateTimeOffset OccurredAt, IReadOnlyList<LineRow> Lines)
{
    public Money? SummaryTotal { get; init; }
    public Money Total => SummaryTotal ?? (Lines.Count == 0 ? new Money(0) : new Money(Lines.Sum(l => l.Total.Amount), Lines[0].UnitPrice.Currency));
}

public sealed record ZoneRow(Guid Id, string Code, string Name);
public sealed record DashboardData(IReadOnlyList<BranchRow> Branches, IReadOnlyList<ProductRow> Products, IReadOnlyList<string> Alerts, TransactionReport? SalesSummary);
