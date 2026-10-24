using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ApexPost.Web.Configuration;

namespace ApexPost.Web.Middleware;

public sealed class ApiKeyMiddleware
{
    private const string HeaderName = "X-API-Key";
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<WebSecurityOptions> _options;

    public ApiKeyMiddleware(RequestDelegate next, IOptionsMonitor<WebSecurityOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/integration"))
        {
            await _next(context);
            return;
        }

        var configuredKeys = _options.CurrentValue.IntegrationApiKeys ?? Array.Empty<string>();
        if (configuredKeys.Length == 0)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { title = "API key configuration missing", status = 500, detail = "The integration API key has not been configured." });
            return;
        }

        var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        var valid = !string.IsNullOrEmpty(supplied) && configuredKeys.Any(configured => ConstantTimeEquals(supplied, configured));
        if (!valid)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { title = "Unauthorized", status = 401 });
            return;
        }

        await _next(context);
    }

    public static bool ConstantTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
