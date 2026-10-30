using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ApexPost.Web.Models;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ApexPost.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly DemoAccountOptions _options;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IOptions<DemoAccountOptions> options, ILogger<AccountController> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        var account = _options.Accounts.FirstOrDefault(x =>
            string.Equals(x.Username, model.Username, StringComparison.OrdinalIgnoreCase));
        var expected = GetConfiguredPassword(account);
        if (account is null || string.IsNullOrEmpty(expected) || !ConstantTimeEquals(model.Password, expected))
        {
            _logger.LogWarning("Failed login for {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, account.DisplayName),
            new(ClaimTypes.NameIdentifier, account.Username),
            new(ClaimTypes.Role, account.Role),
            new(WebClaimTypes.BranchId, account.BranchId),
            new(WebClaimTypes.StaffId, account.StaffId),
            new(WebClaimTypes.BranchCode, account.BranchId),
            new(WebClaimTypes.Role, account.Role)
        };
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return !string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
            ? Redirect(model.ReturnUrl)
            : RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Denied() => StatusCode(StatusCodes.Status403Forbidden);

    private static string? GetConfiguredPassword(DemoAccount? account)
    {
        if (account is null) return null;
        if (!string.IsNullOrWhiteSpace(account.Password)) return account.Password;
        return string.IsNullOrWhiteSpace(account.PasswordEnvironmentVariable)
            ? null
            : Environment.GetEnvironmentVariable(account.PasswordEnvironmentVariable);
    }

    private static bool ConstantTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
}
