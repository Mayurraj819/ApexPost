using System.Diagnostics;
using System.Security.Claims;
using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Entities;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApexPost.Web.Filters;
public sealed class AuditActionFilter : IAsyncActionFilter
{
    private readonly ILogger<AuditActionFilter> _logger;
    private readonly IAuditStore _auditStore;
    public AuditActionFilter(ILogger<AuditActionFilter> logger, IAuditStore auditStore)
    {
        _logger = logger;
        _auditStore = auditStore;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var timer = Stopwatch.StartNew();
        ActionExecutedContext? executed = null;
        try
        {
            executed = await next();
        }
        finally
        {
            timer.Stop();
            var http = context.HttpContext;
            var controller = context.RouteData.Values["controller"]?.ToString() ?? "unknown";
            var action = context.RouteData.Values["action"]?.ToString() ?? "unknown";
            var staffId = http.User.FindFirstValue(WebClaimTypes.StaffId);
            var role = http.User.FindFirstValue(ClaimTypes.Role);
            var branch = http.User.FindFirstValue(WebClaimTypes.BranchId);
            var correlation = http.Items["X-Correlation-ID"]?.ToString() ?? http.TraceIdentifier;
            // ONLY route IDs, never model-bound action arguments or request bodies.
            var safeRouteKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "id",
                "transactionId",
                "branchId",
                "productId",
                "counterId"
            };
            var safeRoutes = string.Join(";", context.RouteData.Values.Where(x => safeRouteKeys.Contains(x.Key) && Guid.TryParse(x.Value?.ToString(), out _)).Select(x => $"{x.Key}={x.Value}"));
            // Response completion sees the final MVC result/error status. Own context in EfAuditStore.
            var outcome = executed?.Exception is null ? "completed" : "faulted";
            var exceptionType = executed?.Exception?.GetType().Name;
            http.Response.OnCompleted(async () =>
            {
                try
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    var entry = new AuditEntry(DateTimeOffset.UtcNow, controller, action, staffId, role, branch, correlation, http.Response.StatusCode, timer.Elapsed.TotalMilliseconds, safeRoutes, outcome, exceptionType);
                    await _auditStore.AppendAsync(entry, timeout.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Audit persistence failed. Type={Type} Correlation={Correlation}", ex.GetType().Name, correlation);
                }
            });
        }
    }
}
