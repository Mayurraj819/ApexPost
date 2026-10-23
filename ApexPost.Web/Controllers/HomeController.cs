using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;

[Authorize]
public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    [AllowAnonymous]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        return Problem(detail: feature?.Error.Message);
    }
}
