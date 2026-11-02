using System.Security.Claims;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;
[Authorize]
public sealed class OperationalAlertsController(IPlatformQueries queries) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        Guid? branchId = null;
        if (!User.IsInRole("Administrator"))
        {
            if (!Guid.TryParse(User.FindFirstValue(WebClaimTypes.BranchId), out var id))
                return Forbid();
            branchId = id;
        }

        return View(await queries.AlertsAsync(branchId, ct));
    }
}
