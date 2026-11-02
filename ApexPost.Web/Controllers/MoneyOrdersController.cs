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
public sealed class MoneyOrdersController(IMoneyOrderService service) : Controller
{
    [HttpGet]
    public IActionResult New() => View(new MoneyOrderForm());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MoneyOrderForm model, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(WebClaimTypes.BranchId), out var branch) || !Guid.TryParse(User.FindFirstValue(WebClaimTypes.StaffId), out var staff))
            return Forbid();
        if (model.DocumentNumber != model.ConfirmDocumentNumber)
            ModelState.AddModelError(nameof(model.ConfirmDocumentNumber), "Document numbers do not match.");
        if (ModelState.IsValid)
        {
            try
            {
                var result = await service.CreateAsync(new MoneyOrderRequest(branch, staff, new Money(model.Amount), model.PayeeName, IdentityDocument.Create(model.DocumentType, model.DocumentNumber)), ct);
                TempData["MoneyOrderReference"] = result.Reference.ToString();
                TempData["MaskedDocument"] = result.MaskedNumber;
                return RedirectToAction(nameof(Completed));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (DomainRuleException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }

        // Never echo a raw identity number back into HTML or TempData on validation failure.
        ModelState.Remove(nameof(model.DocumentNumber));
        ModelState.Remove(nameof(model.ConfirmDocumentNumber));
        model.DocumentNumber = "";
        model.ConfirmDocumentNumber = "";
        return View("New", model);
    }

    [HttpGet]
    public IActionResult Completed() => View();
}
