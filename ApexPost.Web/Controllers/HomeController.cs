using System.Security.Claims;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Web.Models;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;
[Authorize]
public sealed class HomeController(IPlatformQueries queries) : Controller
{
    public IActionResult Index() => RedirectToAction(nameof(Dashboard));
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(WebClaimTypes.BranchId), out var branchId))
            return Forbid();
        var d = await queries.DashboardAsync(branchId, ct);
        return View(new DashboardViewModel { Branches = d.Branches, Products = d.Products, Alerts = d.Alerts, SalesSummary = d.SalesSummary });
    }
}
