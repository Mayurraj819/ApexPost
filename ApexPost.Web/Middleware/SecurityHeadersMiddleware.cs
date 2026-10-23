using Microsoft.Extensions.Options;
using ApexPost.Web.Configuration;

namespace ApexPost.Web.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<WebSecurityOptions> _options;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<WebSecurityOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'none'; form-action 'self'";
        if (context.Request.IsHttps)
        {
            context.Response.Headers["Strict-Transport-Security"] = $"max-age={Math.Max(1, _options.Value.HstsMaxAgeDays) * 86400}; includeSubDomains";
        }

        await _next(context);
    }
}
