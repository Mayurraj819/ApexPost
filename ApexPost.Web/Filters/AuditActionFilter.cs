using System.Diagnostics;
using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Entities;
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
        var stopwatch = Stopwatch.StartNew();
        var executed = await next();
        stopwatch.Stop();

        var controller = context.RouteData.Values["controller"]?.ToString() ?? "unknown";
        var action = context.RouteData.Values["action"]?.ToString() ?? "unknown";
        var staffId = executed.HttpContext.User.FindFirst("apexpost:staff_id")?.Value ?? executed.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var role = executed.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? executed.HttpContext.User.FindFirst("apexpost:role")?.Value;
        var branchId = executed.HttpContext.User.FindFirst("apexpost:branch_id")?.Value ?? executed.HttpContext.User.FindFirst("apexpost:branch_code")?.Value;
        var routeValues = string.Join(";", context.ActionArguments.Select(pair => $"{pair.Key}={pair.Value}"));
        var correlationId = executed.HttpContext.Items["X-Correlation-ID"]?.ToString() ?? Guid.Empty.ToString();

        var auditEntry = new AuditEntry(
            DateTimeOffset.UtcNow,
            controller,
            action,
            staffId,
            role,
            branchId,
            correlationId,
            executed.HttpContext.Response.StatusCode,
            stopwatch.Elapsed.TotalMilliseconds,
            routeValues,
            executed.Exception is null ? "completed" : "faulted",
            executed.Exception is null ? null : executed.Exception.GetType().Name);

        await _auditStore.AppendAsync(auditEntry, context.HttpContext.RequestAborted);

        _logger.LogInformation("MVC action audited. Controller={Controller} Action={Action} StatusCode={StatusCode} StaffId={StaffId} Role={Role} BranchId={BranchId} CorrelationId={CorrelationId} DurationMs={DurationMs}",
            controller,
            action,
            executed.HttpContext.Response.StatusCode,
            staffId ?? "anonymous",
            role ?? "unknown",
            branchId ?? "unknown",
            correlationId,
            stopwatch.Elapsed.TotalMilliseconds);
    }
}
