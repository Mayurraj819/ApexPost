using System.Security.Claims;
using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Enums;
using ApexPost.Web.Models;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;

[Authorize]
public sealed class ShippingLabelsController : Controller
{
    private readonly ITransactionRepository _transactions;

    public ShippingLabelsController(ITransactionRepository transactions) => _transactions = transactions;

    [HttpGet]
    public async Task<IActionResult> Print(Guid transactionId, int lineNumber = 1, CancellationToken cancellationToken = default)
    {
        if (lineNumber < 1) return BadRequest("Line number must be positive.");

        var transaction = await _transactions.GetByIdAsync(transactionId, cancellationToken);
        if (transaction is null) return NotFound();

        var isAdministrator = User.IsInRole("Administrator");
        var branchClaim = User.FindFirstValue(WebClaimTypes.BranchId);
        if (!isAdministrator &&
            (!Guid.TryParse(branchClaim, out var branchId) || branchId != transaction.BranchId))
            return Forbid();

        if (transaction.Status != TransactionStatus.Paid)
            return BadRequest("A shipping label can only be printed for a paid transaction.");

        var shipping = transaction.Lines
            .Where(line => line.Type == TransactionLineType.PostalService)
            .Skip(lineNumber - 1)
            .FirstOrDefault();

        if (shipping is null) return NotFound("Shipping line not found.");

        return View(new ShippingLabelViewModel(
            transaction.Id, transaction.BranchId, transaction.OccurredAt,
            shipping.Code, shipping.ShippingLevel, shipping.Total.Amount,
            shipping.Total.Currency, lineNumber));
    }
}
