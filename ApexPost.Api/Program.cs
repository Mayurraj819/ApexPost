using ApexPost.Api.Errors;
using ApexPost.Infrastructure;
using ApexPost.Infrastructure.Delivery;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ApexPost.Application.Abstractions.Services.ICurrentActor, ApexPost.Api.Services.ApiCurrentActor>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context => context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
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
app.MapControllers();
app.Run();
/// <summary>API application entry point for integration tests.</summary>
public partial class ApiProgram;
