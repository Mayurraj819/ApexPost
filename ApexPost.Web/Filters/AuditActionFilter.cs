using Microsoft.AspNetCore.Mvc.Filters;

namespace ApexPost.Web.Filters;

public sealed class AuditActionFilter : IAsyncActionFilter
{
    private readonly ILogger<AuditActionFilter> _logger;

    public AuditActionFilter(ILogger<AuditActionFilter> logger) => _logger = logger;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        _logger.LogInformation("MVC action audited. Controller={Controller} Action={Action} StatusCode={StatusCode} User={User} CorrelationId={CorrelationId}",
            context.RouteData.Values["controller"],
            context.RouteData.Values["action"],
            executed.HttpContext.Response.StatusCode,
            executed.HttpContext.User.Identity?.Name ?? "anonymous",
            executed.HttpContext.Items["X-Correlation-ID"]?.ToString());
    }
}
