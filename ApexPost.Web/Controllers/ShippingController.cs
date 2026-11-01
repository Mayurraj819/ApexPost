using System.Security.Claims;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Contracts;
using ApexPost.Domain.ValueObjects;
using ApexPost.Web.Models;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;
[Authorize]
public sealed class ShippingController(IPlatformQueries queries, IShippingCommands commands) : Controller
{
    [HttpGet]
    public async Task<IActionResult> New(Guid transactionId, CancellationToken ct)
    {
        var sale = await queries.TransactionAsync(transactionId, ct);
        if (sale is null)
            return NotFound();
        if (!Guid.TryParse(User.FindFirstValue(WebClaimTypes.BranchId), out var branch) || branch != sale.BranchId)
            return Forbid();
        ViewBag.Zones = await queries.ZonesAsync(ct);
        return View(new ShipmentRequest { TransactionId = transactionId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(ShipmentRequest model, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(WebClaimTypes.BranchId), out var branch))
            return Forbid();
        var sale = await queries.TransactionAsync(model.TransactionId, ct);
        if (sale is null)
            return NotFound();
        if (sale.BranchId != branch)
            return Forbid();
        if (model.ZoneId == Guid.Empty)
            ModelState.AddModelError(nameof(model.ZoneId), "Select a zone.");
        if (ModelState.IsValid)
        {
            try
            {
                await commands.AddAsync(new AddShipmentCommand(model.TransactionId, branch, model.ZoneId, new Weight(model.WeightGrams), model.ServiceLevel, model.Recipient, model.Street, model.City, model.PostalCode), ct);
                return RedirectToAction("Details", "Transactions", new { id = model.TransactionId });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }

        ViewBag.Zones = await queries.ZonesAsync(ct);
        return View("New", model);
    }
}
