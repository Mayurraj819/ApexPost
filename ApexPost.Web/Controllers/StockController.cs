using System.Security.Claims;
using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Entities;
using ApexPost.Domain.ValueObjects;
using ApexPost.Web.Models;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ApexPost.Web.Controllers;
[Authorize]
public sealed class StockController : Controller
{
    private readonly IBranchStockRepository _stock;
    private readonly IProductRepository _products;
    public StockController(IBranchStockRepository stock, IProductRepository products)
    { _stock = stock; _products = products; }
    private bool TryBranch(out Guid id) =>
        Guid.TryParse(User.FindFirstValue(WebClaimTypes.BranchId), out id);

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!TryBranch(out var branch)) return Forbid();
        var products = await _products.ListAsync(ct);
        var stock = await _stock.ListAsync(ct);
        ViewBag.Products = products;
        ViewBag.Stock = stock.Where(s => s.BranchId == branch).ToDictionary(s => s.ProductId, s => s.Quantity.Value);
        return View(new StockEditorModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = WebPolicies.BranchManagerOnly)]
    public async Task<IActionResult> Set(StockEditorModel model, CancellationToken ct)
    {
        if (!TryBranch(out var branch)) return Forbid();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Select a product and enter a valid nonnegative quantity.";
            return RedirectToAction(nameof(Index));
        }
        if (await _products.GetByIdAsync(model.ProductId, ct) is null)
        {
            TempData["Error"] = "Product not found.";
            return RedirectToAction(nameof(Index));
        }
        var existing = await _stock.GetByBranchAndProductAsync(branch, model.ProductId, ct);
        if (existing is null)
            await _stock.AddAsync(new BranchStock(Guid.NewGuid(), branch, model.ProductId, new Quantity(model.Quantity)), ct);
        else
        {
            var diff = model.Quantity - existing.Quantity.Value;
            if (diff > 0) existing.Increase(diff);
            if (diff < 0) existing.Decrease(-diff);
            await _stock.SaveAsync(existing, ct);
        }
        TempData["Success"] = "Stock updated.";
        return RedirectToAction(nameof(Index));
    }
}
