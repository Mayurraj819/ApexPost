using ApexPost.Domain.Entities;

namespace ApexPost.Web.Models;

public sealed class CatalogViewModel
{
    public string BranchName { get; init; } = "All branches";
    public IReadOnlyList<CatalogItemViewModel> Items { get; init; } = Array.Empty<CatalogItemViewModel>();
}

public sealed class CatalogItemViewModel
{
    public Product Product { get; init; } = null!;
    public int Stock { get; init; }
    public bool LowStock => Stock <= 3;
}
