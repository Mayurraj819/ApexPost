using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Entities;
using ApexPost.Domain.ValueObjects;
using ApexPost.Web.Models;
using ApexPost.Web.Services;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ApexPost.Web.Controllers;
[Authorize]
public sealed class CatalogController : Controller
{
    private readonly IProductRepository _products;
    private readonly ITransactionRepository _transactions;
    private readonly IBranchStockRepository _stock;
    public CatalogController(IProductRepository products, ITransactionRepository transactions, IBranchStockRepository stock)
    { _products = products; _transactions = transactions; _stock = stock; }

    public async Task<IActionResult> Index(string? q, CancellationToken ct)
    {
        var items = await _products.ListAsync(ct);
        if (!string.IsNullOrWhiteSpace(q))
            items = items.Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                     p.Sku.Value.Contains(q, StringComparison.OrdinalIgnoreCase)).ToArray();
        ViewBag.Search = q;
        return View(items.OrderBy(p => p.Name).ToArray());
    }

    [Authorize(Policy = WebPolicies.BranchManagerOnly)]
    [HttpGet] public IActionResult Create() => View("Edit", new ProductEditorModel());

    [Authorize(Policy = WebPolicies.BranchManagerOnly)]
    [HttpGet] public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var p = await _products.GetByIdAsync(id, ct);
        if (p is null) return NotFound();
        return View(new ProductEditorModel { Id = p.Id, Sku = p.Sku.Value,
            Name = p.Name, Price = p.UnitPrice.Amount, Category = p.Category });
    }

    [Authorize(Policy = WebPolicies.BranchManagerOnly)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ProductEditorModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("Edit", model);
        var all = await _products.ListAsync(ct);
        if (all.Any(p => p.Id != model.Id &&
            p.Sku.Value.Equals(model.Sku.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(model.Sku), "SKU already exists.");
            return View("Edit", model);
        }
        try
        {
            var p = new Product(model.Id == Guid.Empty ? Guid.NewGuid() : model.Id,
                new Sku(model.Sku), model.Name, new Money(model.Price), model.Category);
            if (model.Id == Guid.Empty) await _products.AddAsync(p, ct);
            else
            {
                if (await _products.GetByIdAsync(model.Id, ct) is null) return NotFound();
                await _products.SaveAsync(p, ct);
            }
            TempData["Success"] = "Product saved.";
            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("Edit", model);
        }
    }

    [Authorize(Policy = WebPolicies.BranchManagerOnly)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(id, ct);
        if (product is null) return NotFound();
        var transactions = await _transactions.ListAsync(ct);
        var stock = await _stock.ListAsync(ct);
        if (transactions.Any(t => t.Lines.Any(l => l.ProductId == id)) ||
            stock.Any(s => s.ProductId == id && s.Quantity.Value > 0))
        {
            TempData["Error"] = "Cannot delete: product is referenced by a transaction or has stock.";
            return RedirectToAction(nameof(Index));
        }
        // Delete empty stock records first to avoid orphaned references.
        foreach (var item in stock.Where(s => s.ProductId == id))
            await _stock.DeleteAsync(item.Id, ct);
        await _products.DeleteAsync(id, ct);
        TempData["Success"] = "Product deleted.";
        return RedirectToAction(nameof(Index));
    }
}
