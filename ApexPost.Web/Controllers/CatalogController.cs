using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Contracts;
using ApexPost.Web.Models;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;
[Authorize]
public sealed class CatalogController(IPlatformQueries queries, ICatalogCommands commands, ISqlOperations sql) : Controller
{
    public async Task<IActionResult> Index(string? q, CancellationToken ct)
    {
        ViewBag.Search = q;
        return View(await queries.ProductsAsync(q, ct));
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var p = await queries.ProductAsync(id, ct);
        return p is null ? NotFound() : View(p);
    }

    [Authorize(Policy = WebPolicies.BranchManagerOnly)]
    [HttpGet]
    public IActionResult Create() => View("Edit", new ProductEditorModel());
    [Authorize(Policy = WebPolicies.BranchManagerOnly)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var p = await queries.ProductAsync(id, ct);
        if (p is null)
            return NotFound();
        return View(new ProductEditorModel { Id = p.Id, Sku = p.Sku.Value, Name = p.Name, Price = p.UnitPrice.Amount, Category = p.Category });
    }

    [Authorize(Policy = WebPolicies.BranchManagerOnly)]
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(ProductEditorModel model, CancellationToken ct) => Save(null, model, ct);
    [Authorize(Policy = WebPolicies.BranchManagerOnly)]
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit([FromRoute] Guid id, ProductEditorModel model, CancellationToken ct) => Save(id, model, ct);
    private async Task<IActionResult> Save(Guid? id, ProductEditorModel model, CancellationToken ct)
    {
        model.Id = id ?? Guid.Empty;
        if (!ModelState.IsValid)
            return View("Edit", model);
        try
        {
            await commands.SaveAsync(new SaveProductCommand(id, model.Sku, model.Name, model.Price, model.Category), ct);
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
        try
        {
            await commands.DeleteAsync(id, ct);
            TempData["Success"] = "Product deleted.";
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = WebPolicies.Administrator)]
    public async Task<IActionResult> RevisePrices(PriceRevisionRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            throw new ArgumentException("Factor must be between 0.01 and 10.");
        var count = await sql.RevisePricesAsync(request.Factor, User.Identity?.Name ?? "unknown", ct);
        TempData["Success"] = $"Revised {count} product prices.";
        return RedirectToAction(nameof(Index));
    }
}
