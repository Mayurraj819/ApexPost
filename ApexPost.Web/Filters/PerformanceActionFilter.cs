using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApexPost.Web.Filters;

public sealed class PerformanceActionFilter : IAsyncActionFilter
{
    private readonly ILogger<PerformanceActionFilter> _logger;

    public PerformanceActionFilter(ILogger<PerformanceActionFilter> logger) => _logger = logger;

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
            _logger.LogInformation("MVC action timing. Controller={Controller} Action={Action} ElapsedMs={ElapsedMs} CorrelationId={CorrelationId}",
                context.RouteData.Values["controller"],
                context.RouteData.Values["action"],
                stopwatch.Elapsed.TotalMilliseconds,
                context.HttpContext.Items["X-Correlation-ID"]?.ToString());
        }
    }
}
