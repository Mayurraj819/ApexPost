using Microsoft.Extensions.Options;
using ApexPost.Web.Configuration;

namespace ApexPost.Web.Middleware;

public sealed class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<WebSecurityOptions> _options;

    public MaintenanceModeMiddleware(RequestDelegate next, IOptionsMonitor<WebSecurityOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_options.CurrentValue.MaintenanceMode && !context.Request.Path.StartsWithSegments("/health"))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.com/503",
                title = "Service temporarily unavailable",
                status = 503,
                detail = "ApexPost is undergoing maintenance.",
                correlationId = context.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
            });
            return;
        }

        await _next(context);
    }
}
