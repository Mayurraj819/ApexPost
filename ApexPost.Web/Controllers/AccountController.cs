using System.Collections.Concurrent;
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
    private static readonly ConcurrentDictionary<string, (DateTimeOffset WindowStart, int Count)> LoginAttempts = new(StringComparer.OrdinalIgnoreCase);
    private readonly DemoAccountOptions _options;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IOptions<DemoAccountOptions> options, ILogger<AccountController> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        var requestKey = HttpContext.Connection.RemoteIpAddress?.ToString() ?? HttpContext.TraceIdentifier;
        var now = DateTimeOffset.UtcNow;

        var existing = LoginAttempts.GetOrAdd(requestKey, _ => (now, 0));
        if (now - existing.WindowStart > TimeSpan.FromSeconds(60))
        {
            existing = (now, 0);
        }

        var nextCount = existing.Count + 1;
        LoginAttempts[requestKey] = (existing.WindowStart, nextCount);
        if (nextCount > 2)
        {
            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return StatusCode(StatusCodes.Status429TooManyRequests, new { title = "Too Many Requests", status = 429 });
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var account = _options.Accounts.FirstOrDefault(item => string.Equals(item.Username, model.Username, StringComparison.OrdinalIgnoreCase));
        var configuredPassword = account is null ? null : Environment.GetEnvironmentVariable(account.PasswordEnvironmentVariable);
        var valid = account is not null && !string.IsNullOrEmpty(configuredPassword) && ConstantTimeEquals(model.Password, configuredPassword);
        if (!valid)
        {
            _logger.LogWarning("Failed login attempt for configured username. Username={Username} CorrelationId={CorrelationId}", model.Username, HttpContext.Items["X-Correlation-ID"]?.ToString());
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, account!.DisplayName),
            new(ClaimTypes.NameIdentifier, account.Username),
            new(ClaimTypes.Role, account.Role),
            new(WebClaimTypes.BranchId, account.BranchId),
            new(WebClaimTypes.StaffId, account.StaffId),
            new(WebClaimTypes.BranchCode, account.BranchId),
            new(WebClaimTypes.Role, account.Role)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Denied() => StatusCode(StatusCodes.Status403Forbidden);

    private IActionResult RedirectToLocal(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home")!;

    private string GetRateLimitKey() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? HttpContext.TraceIdentifier;

    private static bool ConstantTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
}
