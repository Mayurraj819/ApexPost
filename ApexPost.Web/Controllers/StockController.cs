using System.Security.Claims;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Contracts;
using ApexPost.Web.Models;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;
[Authorize]
public sealed class StockController(IPlatformQueries queries, ICatalogCommands commands, ISqlOperations sql) : Controller
{
    private bool Branch(out Guid id) => Guid.TryParse(User.FindFirstValue(WebClaimTypes.BranchId), out id);
    private async Task Populate(Guid id, CancellationToken ct)
    {
        ViewBag.Products = await queries.ProductsAsync(null, ct);
        var rows = await queries.StockAsync(id, ct);
        ViewBag.Stock = rows.ToDictionary(s => s.ProductId, s => s.Quantity);
        ViewBag.ExistingStock = rows.Where(s => s.Exists).Select(s => s.ProductId).ToHashSet();
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!Branch(out var id))
            return Forbid();
        await Populate(id, ct);
        return View(new StockEditorModel());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = WebPolicies.BranchManagerOnly)]
    public async Task<IActionResult> Set(StockEditorModel model, CancellationToken ct)
    {
        if (!Branch(out var id))
            return Forbid();
        if (model.ProductId == Guid.Empty)
            ModelState.AddModelError(nameof(model.ProductId), "Select a product.");
        if (ModelState.IsValid)
        {
            try
            {
                await commands.SetStockAsync(new SetStockCommand(id, model.ProductId, model.Quantity), ct);
                TempData["Success"] = "Stock updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }

        await Populate(id, ct);
        return View("Index", model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = WebPolicies.BranchManagerOnly)]
    public async Task<IActionResult> Adjust(StockDeltaRequest request, CancellationToken ct)
    {
        if (!Branch(out var branch))
            return Forbid();
        if (!ModelState.IsValid || request.Delta == 0)
        {
            TempData["Error"] = "Choose a product, nonzero delta and reason.";
            return RedirectToAction(nameof(Index));
        }

        var code = await queries.BranchCodeAsync(branch, ct) ?? throw new KeyNotFoundException("Branch not found.");
        var product = await queries.ProductAsync(request.ProductId, ct) ?? throw new KeyNotFoundException("Product not found.");
        await sql.AdjustStockAsync(code, product.Sku.Value, request.Delta, request.Reason, User.Identity?.Name ?? "unknown", ct);
        TempData["Success"] = "Adjustment and history saved atomically.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = WebPolicies.BranchManagerOnly)]
    public async Task<IActionResult> Remove([FromRoute] Guid id, CancellationToken ct)
    {
        if (!Branch(out var branch))
            return Forbid();
        try
        {
            await commands.RemoveEmptyStockAsync(branch, id, ct);
            TempData["Success"] = "Empty stock record removed; history retained.";
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
