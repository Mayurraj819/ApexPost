using ApexPost.Application.Delivery;
using ApexPost.Application.Exceptions;
using ApexPost.Domain.Delivery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Api.Errors;
/// <summary>Shipment API contract or supporting operation.</summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problems, IHostEnvironment environment, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>Shipment API contract or supporting operation.</summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            ShipmentNotFoundException or KeyNotFoundException => 404,
            DeliveryConflictException or ConcurrencyConflictException or UniqueConflictException or DbUpdateConcurrencyException => 409,
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } => 409,
            ArgumentException => 400,
            _ => 500
        };
        if (status == 500)
            logger.LogError(exception, "Unhandled API failure. TraceId={TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = new ProblemDetails { Status = status, Title = Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(status), Detail = status == 500 && !environment.IsDevelopment() ? "An unexpected server error occurred." : exception.Message, Instance = context.Request.Path } });
        return true;
    }
}
