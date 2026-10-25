using System.Diagnostics;
using ApexPost.Web.Configuration;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace ApexPost.Web.Filters;

public sealed class PerformanceActionFilter : IAsyncActionFilter
{
    private readonly ILogger<PerformanceActionFilter> _logger;
    private readonly IOptionsMonitor<WebSecurityOptions> _options;

    public PerformanceActionFilter(
        ILogger<PerformanceActionFilter> logger,
        IOptionsMonitor<WebSecurityOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next();
        }
        finally
        {
            stopwatch.Stop();
            var elapsed = stopwatch.Elapsed.TotalMilliseconds;
            if (elapsed >= Math.Max(1, _options.CurrentValue.SlowActionThresholdMilliseconds))
            {
                _logger.LogWarning(
                    "Slow MVC action {Controller}.{Action}: {ElapsedMs}ms CorrelationId={CorrelationId}",
                    context.RouteData.Values["controller"],
                    context.RouteData.Values["action"],
                    elapsed,
                    context.HttpContext.Items["X-Correlation-ID"]);
            }
        }
    }
}
