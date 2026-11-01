using ApexPost.Application.Exceptions;
using ApexPost.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApexPost.Tests.Web;
[Trait("Category", "Unit")]
public sealed class ProblemDetailsTests
{
    [Theory]
    [InlineData(409)]
    [InlineData(404)]
    [InlineData(400)]
    [InlineData(500)]
    public async Task Exceptions_map_to_sanitized_problem_details(int status)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/test";
        Exception error = status switch
        {
            409 => new ConcurrencyConflictException("Refresh stock."),
            404 => new KeyNotFoundException("Missing."),
            400 => new DomainRuleException("Closed branch."),
            _ => new InvalidOperationException("PRIVATE DETAILS")};
        await new ExceptionHandlingMiddleware(_ => throw error, NullLogger<ExceptionHandlingMiddleware>.Instance).InvokeAsync(context);
        Assert.Equal(status, context.Response.StatusCode);
        Assert.Contains("application/problem+json", context.Response.ContentType!);
        context.Response.Body.Position = 0;
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("PRIVATE DETAILS", json);
    }
}
