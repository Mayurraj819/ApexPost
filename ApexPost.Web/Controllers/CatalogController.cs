using ApexPost.Application.Abstractions.Services;
using ApexPost.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;

[Authorize]
public sealed class CatalogController : Controller
{
    private readonly IProductCatalogCache _catalog;

    public CatalogController(IProductCatalogCache catalog) => _catalog = catalog;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var products = await _catalog.GetAsync(cancellationToken);
        return View(products);
    }
}
