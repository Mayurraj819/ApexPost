using System.Security.Claims;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;

[Authorize]
public sealed class ReportsController : Controller
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var branchIdClaim = User.FindFirstValue(WebClaimTypes.BranchId);
        if (!Guid.TryParse(branchIdClaim, out var branchId))
        {
            return BadRequest("No branch selected for this user.");
        }

        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var report = await _reports.GetTransactionReportAsync(branchId, from, DateTimeOffset.UtcNow, cancellationToken);
        return View(report);
    }
}
