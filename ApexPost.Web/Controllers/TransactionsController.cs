using System.Security.Claims;
using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Domain.ValueObjects;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;

[Authorize]
public sealed class TransactionsController : Controller
{
    private readonly ITransactionProcessingService _processing;
    private readonly ITransactionRepository _repository;
    private readonly ICounterRepository _counters;
    private readonly IProductRepository _products;

    public TransactionsController(
        ITransactionProcessingService processing,
        ITransactionRepository repository,
        ICounterRepository counters,
        IProductRepository products)
    {
        _processing = processing;
        _repository = repository;
        _counters = counters;
        _products = products;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryBranch(out var branchId)) return Forbid();
        var all = await _repository.ListAsync(cancellationToken);
        return View(all.Where(x => x.BranchId == branchId).OrderByDescending(x => x.OccurredAt).ToArray());
    }

    [HttpGet]
    public async Task<IActionResult> New(CancellationToken cancellationToken)
    {
        if (!TryBranch(out var branchId)) return Forbid();
        var counters = await _counters.ListAsync(cancellationToken);
        return View(counters.Where(x => x.BranchId == branchId).ToArray());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(Guid counterId, CancellationToken cancellationToken)
    {
        if (!TryBranch(out var branchId) || !TryStaff(out var staffId)) return Forbid();
        var counter = await _counters.GetByIdAsync(counterId, cancellationToken);
        if (counter is null || counter.BranchId != branchId) return BadRequest("Invalid counter.");
        var transaction = await _processing.StartAsync(branchId, counterId, staffId, DateTimeOffset.UtcNow, cancellationToken);
        return RedirectToAction(nameof(Details), new { id = transaction.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await _repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null) return NotFound();
        if (!TryBranch(out var branchId) || transaction.BranchId != branchId) return Forbid();
        ViewBag.Products = await _products.ListAsync(cancellationToken);
        return View(transaction);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduct(Guid id, Guid productId, int quantity, CancellationToken cancellationToken)
    {
        var transaction = await _repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null) return NotFound();
        if (!TryBranch(out var branchId) || transaction.BranchId != branchId) return Forbid();
        if (quantity <= 0) return BadRequest("Quantity must be positive.");
        await _processing.AddProductAsync(id, productId, new Quantity(quantity), cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await _repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null) return NotFound();
        if (!TryBranch(out var branchId) || transaction.BranchId != branchId) return Forbid();
        await _processing.PayAsync(id, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = WebPolicies.BranchManagerOnly)]
    public async Task<IActionResult> Void(Guid transactionId, string reason, CancellationToken cancellationToken)
    {
        if (!TryStaff(out var managerId) || !TryBranch(out var branchId)) return Forbid();
        var transaction = await _repository.GetByIdAsync(transactionId, cancellationToken);
        if (transaction is null) return NotFound();
        if (transaction.BranchId != branchId) return Forbid();
        if (string.IsNullOrWhiteSpace(reason)) return BadRequest("Reason is required.");
        await _processing.VoidAsync(transactionId, managerId, reason, cancellationToken);
        return RedirectToAction(nameof(Details), new { id = transactionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = WebPolicies.BranchManagerOnly)]
    public async Task<IActionResult> Refund(Guid transactionId, string reason, CancellationToken cancellationToken)
    {
        if (!TryStaff(out var managerId) || !TryBranch(out var branchId)) return Forbid();
        var transaction = await _repository.GetByIdAsync(transactionId, cancellationToken);
        if (transaction is null) return NotFound();
        if (transaction.BranchId != branchId) return Forbid();
        if (string.IsNullOrWhiteSpace(reason)) return BadRequest("Reason is required.");
        await _processing.RefundAsync(transactionId, managerId, reason, cancellationToken);
        return RedirectToAction(nameof(Details), new { id = transactionId });
    }

    private bool TryBranch(out Guid id) =>
        Guid.TryParse(User.FindFirstValue(WebClaimTypes.BranchId), out id);

    private bool TryStaff(out Guid id) =>
        Guid.TryParse(User.FindFirstValue(WebClaimTypes.StaffId), out id);
}
