using System.Diagnostics;

namespace ApexPost.Web.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation("HTTP request completed. Method={Method} Path={Path} StatusCode={StatusCode} ElapsedMs={ElapsedMs} CorrelationId={CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds,
                context.Items[CorrelationIdMiddleware.HeaderName]?.ToString());
        }
    }
}
