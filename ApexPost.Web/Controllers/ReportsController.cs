using System.Security.Claims;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;
[Authorize]
public sealed class ReportsController(IPlatformQueries queries) : Controller
{
    public async Task<IActionResult> Index(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken ct = default)
    {
        Guid? branchId = null;
        if (!User.IsInRole("Administrator"))
        {
            if (!Guid.TryParse(User.FindFirstValue(WebClaimTypes.BranchId), out var id))
                return Forbid();
            branchId = id;
        }

        return View(await queries.ReportsAsync(branchId, from ?? DateTimeOffset.UtcNow.AddDays(-7), to ?? DateTimeOffset.UtcNow, ct));
    }
}
