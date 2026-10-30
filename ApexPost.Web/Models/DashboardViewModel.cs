using ApexPost.Domain.Entities;
using ApexPost.Application.Contracts;

namespace ApexPost.Web.Models;

public sealed class DashboardViewModel
{
    public IReadOnlyList<Branch> Branches { get; init; } = Array.Empty<Branch>();
    public IReadOnlyList<Product> Products { get; init; } = Array.Empty<Product>();
    public IReadOnlyList<string> Alerts { get; init; } = Array.Empty<string>();
    public TransactionReport? SalesSummary { get; init; }
}
