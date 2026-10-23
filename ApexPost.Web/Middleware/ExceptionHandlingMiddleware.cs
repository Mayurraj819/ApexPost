using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ApexPost.Web.Configuration;

namespace ApexPost.Web.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var correlationId = context.Items[CorrelationIdMiddleware.HeaderName]?.ToString();
            _logger.LogError(exception, "Unhandled request exception. CorrelationId={CorrelationId} ExceptionType={ExceptionType}", correlationId, exception.GetType().Name);
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "The request could not be completed. Use the correlation ID when contacting support.",
                Instance = context.Request.Path
            };
            problem.Extensions["correlationId"] = correlationId;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
