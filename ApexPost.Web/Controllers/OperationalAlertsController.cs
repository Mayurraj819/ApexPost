using System.Security.Claims;
using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Enums;
using ApexPost.Web.Models;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;

[Authorize]
public sealed class OperationalAlertsController : Controller
{
    private const int LowStockThreshold = 3;
    private const decimal HighValueThreshold = 10000m;

    private readonly IBranchStockRepository _stock;
    private readonly IProductRepository _products;
    private readonly ITransactionRepository _transactions;
    private readonly IBranchRepository _branches;
    private readonly IMoneyOrderRepository _moneyOrders;

    public OperationalAlertsController(
        IBranchStockRepository stock, IProductRepository products,
        ITransactionRepository transactions, IBranchRepository branches,
        IMoneyOrderRepository moneyOrders)
    {
        _stock = stock;
        _products = products;
        _transactions = transactions;
        _branches = branches;
        _moneyOrders = moneyOrders;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var admin = User.IsInRole("Administrator");
        var branchClaim = User.FindFirstValue(WebClaimTypes.BranchId);
        if (!admin && !Guid.TryParse(branchClaim, out _)) return Forbid();
        var branchId = Guid.TryParse(branchClaim, out var parsed) ? parsed : Guid.Empty;

        var stocksTask = _stock.ListAsync(cancellationToken);
        var productsTask = _products.ListAsync(cancellationToken);
        var transactionsTask = _transactions.ListAsync(cancellationToken);
        var branchesTask = _branches.ListAsync(cancellationToken);
        await Task.WhenAll(stocksTask, productsTask, transactionsTask, branchesTask);

        var products = (await productsTask).ToDictionary(p => p.Id, p => p.Name);
        var lowStock = (await stocksTask)
            .Where(s => (admin || s.BranchId == branchId) && s.Quantity.Value <= LowStockThreshold)
            .Select(s => new LowStockAlert(s.BranchId, s.ProductId,
                products.GetValueOrDefault(s.ProductId, "Unknown product"), s.Quantity.Value))
            .OrderBy(s => s.Remaining).ToArray();

        var corrections = (await transactionsTask)
            .Where(t => (admin || t.BranchId == branchId) &&
                t.Status is TransactionStatus.Voided or TransactionStatus.Refunded)
            .Select(t => new CorrectionAlert(t.Id, t.BranchId, t.Status.ToString(),
                t.StatusReason, t.OccurredAt))
            .OrderByDescending(t => t.OccurredAt).Take(100).ToArray();

        var branchIds = (await branchesTask)
            .Where(b => admin || b.Id == branchId).Select(b => b.Id).ToArray();
        var moneyOrderLists = await Task.WhenAll(branchIds.Select(
            id => _moneyOrders.ListByBranchAsync(id, cancellationToken)));
        var highValue = moneyOrderLists.SelectMany(x => x)
            .Where(m => m.Amount.Amount >= HighValueThreshold)
            .Select(m => new HighValueMoneyOrderAlert(m.Reference, m.BranchId,
                m.Amount.Amount, m.Amount.Currency, m.CreatedAt))
            .OrderByDescending(m => m.CreatedAt).Take(100).ToArray();

        return View(new OperationalAlertsViewModel(
            lowStock, highValue, corrections, LowStockThreshold, HighValueThreshold));
    }
}
