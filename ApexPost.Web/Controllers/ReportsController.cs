using System.Security.Claims;
using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Enums;
using ApexPost.Web.Models;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;

[Authorize]
public sealed class ReportsController : Controller
{
    private readonly ITransactionRepository _transactions;
    private readonly IBranchStockRepository _stock;
    private readonly IProductRepository _products;
    private readonly IBranchRepository _branches;
    private readonly IMoneyOrderRepository _moneyOrders;

    public ReportsController(ITransactionRepository transactions, IBranchStockRepository stock,
        IProductRepository products, IBranchRepository branches, IMoneyOrderRepository moneyOrders)
    {
        _transactions = transactions;
        _stock = stock;
        _products = products;
        _branches = branches;
        _moneyOrders = moneyOrders;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTimeOffset? from = null, DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var isAdmin = User.IsInRole("Administrator");
        var branchClaim = User.FindFirstValue(WebClaimTypes.BranchId);
        if (!isAdmin && !Guid.TryParse(branchClaim, out _)) return Forbid();
        var ownBranch = Guid.TryParse(branchClaim, out var parsed) ? parsed : Guid.Empty;
        var end = to ?? DateTimeOffset.UtcNow;
        var start = from ?? end.AddDays(-7);
        if (start > end) return BadRequest("From must be before To.");
        if (end - start > TimeSpan.FromDays(366)) return BadRequest("Maximum reporting period is 366 days.");

        var transactionTask = _transactions.ListAsync(cancellationToken);
        var stockTask = _stock.ListAsync(cancellationToken);
        var productTask = _products.ListAsync(cancellationToken);
        var branchTask = _branches.ListAsync(cancellationToken);
        await Task.WhenAll(transactionTask, stockTask, productTask, branchTask);

        var paid = (await transactionTask).Where(t =>
            (isAdmin || t.BranchId == ownBranch) && t.Status == TransactionStatus.Paid &&
            t.OccurredAt >= start && t.OccurredAt <= end).ToArray();

        var sales = paid.SelectMany(t => t.Lines.Select(l => new
            { t.BranchId, Day = DateOnly.FromDateTime(t.OccurredAt.Date), Line = l }))
            .GroupBy(x => new { x.BranchId, x.Day, x.Line.UnitPrice.Currency })
            .Select(g => new SalesSplitRow(g.Key.BranchId, g.Key.Day,
                g.Where(x => x.Line.Type == TransactionLineType.RetailProduct).Sum(x => x.Line.Total.Amount),
                g.Where(x => x.Line.Type != TransactionLineType.RetailProduct).Sum(x => x.Line.Total.Amount),
                g.Key.Currency))
            .OrderByDescending(x => x.Day).ToArray();

        var names = (await productTask).ToDictionary(p => p.Id, p => p.Name);
        var lowStock = (await stockTask)
            .Where(s => (isAdmin || s.BranchId == ownBranch) && s.Quantity.Value <= 3)
            .Select(s => new StockWarningRow(s.BranchId, s.ProductId,
                names.GetValueOrDefault(s.ProductId, "Unknown"), s.Quantity.Value))
            .OrderBy(x => x.Quantity).ToArray();

        var shipping = paid.SelectMany(t => t.Lines
            .Where(l => l.Type == TransactionLineType.PostalService)
            .Select(l => new { t.BranchId, Level = l.ShippingLevel?.ToString() ?? "Unspecified", Line = l }))
            .GroupBy(x => new { x.BranchId, x.Level })
            .Select(g => new ShippingVolumeRow(g.Key.BranchId, g.Key.Level,
                g.Sum(x => x.Line.Quantity.Value), g.Sum(x => x.Line.Total.Amount)))
            .OrderBy(x => x.BranchId).ThenBy(x => x.ServiceLevel).ToArray();

        var branchIds = (await branchTask).Where(b => isAdmin || b.Id == ownBranch)
            .Select(b => b.Id).ToArray();
        var moneyOrderLists = await Task.WhenAll(branchIds.Select(
            id => _moneyOrders.ListByBranchAsync(id, cancellationToken)));
        var moneyOrders = moneyOrderLists.SelectMany(x => x)
            .Where(m => m.CreatedAt >= start && m.CreatedAt <= end)
            .GroupBy(m => m.BranchId)
            .Select(g => new MoneyOrderVolumeRow(g.Key, g.Count(),
                g.Sum(x => x.Amount.Amount),
                g.Count(x => !string.IsNullOrWhiteSpace(x.IdentityFingerprint))))
            .OrderBy(x => x.BranchId).ToArray();

        return View(new FullReportsViewModel(sales, lowStock, shipping, moneyOrders, start, end));
    }
}
