using System.Security.Claims;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Web.Models;
using ApexPost.Web.Security;
using ApexPost.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;

[Authorize]
public sealed class HomeController : Controller
{
    private readonly IBranchService _branches;
    private readonly IInventoryService _inventory;
    private readonly IProductCatalogCache _catalogCache;
    private readonly IReportService _reports;

    public HomeController(IBranchService branches, IInventoryService inventory, IProductCatalogCache catalogCache, IReportService reports)
    {
        _branches = branches;
        _inventory = inventory;
        _catalogCache = catalogCache;
        _reports = reports;
    }

    public IActionResult Index() => RedirectToAction(nameof(Dashboard));

    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var branchTask = _branches.ListAsync(cancellationToken);
        var productsTask = _catalogCache.GetAsync(cancellationToken);
        var selectedBranchId = User.FindFirstValue(WebClaimTypes.BranchId);
        var branchInventoryTask = Guid.TryParse(selectedBranchId, out var branchId)
            ? _inventory.ListByBranchAsync(branchId, cancellationToken)
            : Task.FromResult<IReadOnlyList<ApexPost.Domain.Entities.BranchStock>>(Array.Empty<ApexPost.Domain.Entities.BranchStock>());

        await Task.WhenAll(branchTask, productsTask.AsTask(), branchInventoryTask);

        var branches = await branchTask;
        var products = await productsTask;
        var currentInventory = await branchInventoryTask;
        var productById = products.ToDictionary(item => item.Id, item => item);
        var alerts = currentInventory
            .Where(item => item.Quantity.Value <= 3)
            .Select(item => productById.TryGetValue(item.ProductId, out var product)
                ? $"Low stock: {product.Name} ({item.Quantity.Value} remaining)"
                : "Low stock alert")
            .ToArray();

        var summary = Guid.TryParse(selectedBranchId, out var currentBranchId) && branches.Any(branch => branch.Id == currentBranchId)
            ? await _reports.GetTransactionReportAsync(currentBranchId, DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, cancellationToken)
            : null;

        var model = new DashboardViewModel
        {
            Branches = branches,
            Products = products,
            Alerts = alerts,
            SalesSummary = summary
        };

        return View(model);
    }

    [AllowAnonymous]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        return Problem(detail: feature?.Error.Message);
    }
}
