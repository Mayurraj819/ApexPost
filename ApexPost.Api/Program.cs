using ApexPost.Api.Security;
using ApexPost.Api.Errors;
using ApexPost.Api.OpenApi;
using ApexPost.Infrastructure;
using ApexPost.Infrastructure.Delivery;
using Asp.Versioning;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ApexPost.Application.Abstractions.Services.ICurrentActor, ApexPost.Api.Services.ApiCurrentActor>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context => context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddCourierSecurity(builder.Configuration);
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddMvc().AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
// Exactly one generator: Microsoft.AspNetCore.OpenApi. Swagger UI only renders its documents.
builder.Services.AddOpenApi("v1", options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
    options.ShouldInclude = description => description.GroupName == "v1";
    options.AddOperationTransformer((operation, context, ct) =>
    {
        operation.Deprecated = true;
        return Task.CompletedTask;
    });
    options.AddDocumentTransformer<LogisticsDocumentTransformer>();
});
builder.Services.AddOpenApi("v2", options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
    options.ShouldInclude = description => description.GroupName == "v2";
    options.AddDocumentTransformer<LogisticsDocumentTransformer>();
});
var connection = builder.Configuration.GetConnectionString("ApexPost") ?? throw new InvalidOperationException("Configure ConnectionStrings:ApexPost in ApexPost.Api user-secrets.");
builder.Services.AddInfrastructure(connection);
builder.Services.AddShipmentApiServices();
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages(async status =>
{
    var problems = status.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
    await problems.WriteAsync(new ProblemDetailsContext { HttpContext = status.HttpContext, ProblemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = status.HttpContext.Response.StatusCode, Title = Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(status.HttpContext.Response.StatusCode) } });
});
if (!app.Environment.IsDevelopment())
    app.UseHsts();
app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("/openapi/v1.json", "v1 (deprecated)");
        options.SwaggerEndpoint("/openapi/v2.json", "v2");
    });
}

app.UseAuthentication();
app.UseAuthorization();
if(app.Environment.IsDevelopment())
    app.MapPost("/dev/token",(DevTokenRequest request,HttpContext context,DevelopmentTokens tokens)=>tokens.Issue(request,context)).AllowAnonymous().ExcludeFromDescription();
app.MapControllers();
app.Run();
/// <summary>API application entry point for integration tests.</summary>
public partial class ApiProgram;
