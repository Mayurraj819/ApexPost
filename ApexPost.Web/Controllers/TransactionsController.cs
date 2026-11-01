using System.Security.Claims;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Contracts;
using ApexPost.Application.Exceptions;
using ApexPost.Domain.ValueObjects;
using ApexPost.Web.Models;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;
[Authorize]
public sealed class TransactionsController(ITransactionProcessingService processing, IPlatformQueries queries) : Controller
{
    private bool Actor(out Guid branch, out Guid staff)
    {
        return Guid.TryParse(User.FindFirstValue(WebClaimTypes.BranchId), out branch) & Guid.TryParse(User.FindFirstValue(WebClaimTypes.StaffId), out staff);
    }

    private async Task<TransactionRow?> Owned(Guid id, Guid branch, CancellationToken ct)
    {
        var sale = await queries.TransactionAsync(id, ct);
        return sale?.BranchId == branch ? sale : null;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!Actor(out var branch, out _))
            return Forbid();
        return View(await queries.TransactionsAsync(branch, ct));
    }

    [HttpGet]
    public async Task<IActionResult> New(CancellationToken ct)
    {
        if (!Actor(out var branch, out _))
            return Forbid();
        ViewBag.Products = await queries.ProductsAsync(null, ct);
        return View(await queries.CountersAsync(branch, ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(StartTransactionRequest request, CancellationToken ct)
    {
        if (!Actor(out var branch, out var staff))
            return Forbid();
        if (request.CounterId == Guid.Empty || !ModelState.IsValid)
        {
            TempData["Error"] = "Select a valid counter.";
            return RedirectToAction(nameof(New));
        }

        try
        {
            var sale = await processing.StartAsync(branch, request.CounterId, staff, DateTimeOffset.UtcNow, ct);
            return RedirectToAction(nameof(Details), new { id = sale.Id });
        }
        catch (DomainRuleException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(New));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        if (!Actor(out var branch, out _))
            return Forbid();
        var sale = await queries.TransactionAsync(id, ct);
        if (sale is null)
            return NotFound();
        if (sale.BranchId != branch)
            return Forbid();
        ViewBag.Products = await queries.ProductsAsync(null, ct);
        return View(sale);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduct([FromRoute] Guid id, AddProductRequest request, CancellationToken ct)
    {
        if (!Actor(out var branch, out _))
            return Forbid();
        if (await Owned(id, branch, ct)is null)
            return NotFound();
        if (!ModelState.IsValid || request.ProductId == Guid.Empty)
            return ValidationProblem(ModelState);
        await processing.AddProductAsync(id, request.ProductId, new Quantity(request.Quantity), ct);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay([FromRoute] Guid id, PaymentRequest request, CancellationToken ct)
    {
        if (!Actor(out var branch, out _))
            return Forbid();
        if (await Owned(id, branch, ct)is null)
            return NotFound();
        if (!request.Confirm)
            throw new ArgumentException("Confirm payment.");
        await processing.PayAsync(id, ct);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = WebPolicies.BranchManagerOnly)]
    public async Task<IActionResult> Void([FromRoute] Guid id, CorrectionRequest request, CancellationToken ct) => await Correct(id, request, false, ct);
    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = WebPolicies.BranchManagerOnly)]
    public async Task<IActionResult> Refund([FromRoute] Guid id, CorrectionRequest request, CancellationToken ct) => await Correct(id, request, true, ct);
    private async Task<IActionResult> Correct(Guid id, CorrectionRequest request, bool refund, CancellationToken ct)
    {
        if (!Actor(out var branch, out var staff))
            return Forbid();
        if (await Owned(id, branch, ct)is null)
            return NotFound();
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        if (refund)
            await processing.RefundAsync(id, staff, request.Reason, ct);
        else
            await processing.VoidAsync(id, staff, request.Reason, ct);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutRequest request, CancellationToken ct)
    {
        if (!Actor(out var branch, out var staff))
            return Forbid();
        if (!ModelState.IsValid || request.CounterId == Guid.Empty || request.Lines.Any(l => l.ProductId == Guid.Empty))
            return ValidationProblem(ModelState);
        var id = await processing.CheckoutAsync(new CheckoutCommand(branch, request.CounterId, staff, DateTimeOffset.UtcNow, request.Lines.Select(l => new CheckoutLine(l.ProductId, l.Quantity)).ToArray()), ct);
        return RedirectToAction(nameof(Details), new { id });
    }
}
