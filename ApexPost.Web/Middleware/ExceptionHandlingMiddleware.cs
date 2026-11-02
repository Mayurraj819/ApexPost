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
        catch (Exception exception)when (!context.Response.HasStarted)
        {
            var correlationId = context.Items[CorrelationIdMiddleware.HeaderName]?.ToString();
            _logger.LogError(exception, "Unhandled request exception. CorrelationId={CorrelationId} ExceptionType={ExceptionType}", correlationId, exception.GetType().Name);
            context.Response.Clear();
            var status = exception switch
            {
                ApexPost.Application.Exceptions.ConcurrencyConflictException => 409,
                ApexPost.Application.Exceptions.UniqueConflictException => 409,
                KeyNotFoundException => 404,
                ApexPost.Application.Exceptions.DomainRuleException => 400,
                ArgumentException => 400,
                _ => 500
            };
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails
            {
                Status = status,
                Title = status == 500 ? "An unexpected error occurred." : "The operation could not be completed.",
                Detail = status == 500 ? "The request could not be completed. Use the correlation ID when contacting support." : exception.Message,
                Instance = context.Request.Path
            };
            problem.Extensions["correlationId"] = correlationId;
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
        }
    }
}
