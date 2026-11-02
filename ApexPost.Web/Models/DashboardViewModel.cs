using ApexPost.Application.Contracts;

namespace ApexPost.Web.Models;
public sealed class DashboardViewModel
{
    public IReadOnlyList<BranchRow> Branches { get; init; } = [];
    public IReadOnlyList<ProductRow> Products { get; init; } = [];
    public IReadOnlyList<string> Alerts { get; init; } = [];
    public TransactionReport? SalesSummary { get; init; }
}
