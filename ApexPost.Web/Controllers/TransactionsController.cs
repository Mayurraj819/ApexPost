using System.Security.Claims;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;

[Authorize]
public sealed class TransactionsController : Controller
{
    private readonly ITransactionProcessingService _transactions;

    public TransactionsController(ITransactionProcessingService transactions) => _transactions = transactions;

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = WebPolicies.BranchManagerOnly)]
    public async Task<IActionResult> Void(Guid transactionId, string reason, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(WebClaimTypes.StaffId), out var managerId))
        {
            return Forbid();
        }

        await _transactions.VoidAsync(transactionId, managerId, reason, cancellationToken);
        return RedirectToAction("Index", "Home");
    }
}
