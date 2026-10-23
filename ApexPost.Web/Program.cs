using System.Threading.RateLimiting;
using ApexPost.Infrastructure;
using ApexPost.Web.Configuration;
using ApexPost.Web.Filters;
using ApexPost.Web.Middleware;
using ApexPost.Web.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var securitySection = builder.Configuration.GetSection("ApexPost:Security");
var securityOptions = securitySection.Get<WebSecurityOptions>() ?? new WebSecurityOptions();
builder.Services.Configure<WebSecurityOptions>(securitySection);
builder.Services.Configure<DemoAccountOptions>(builder.Configuration.GetSection("ApexPost:DemoAccounts"));

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<AuditActionFilter>();
    options.Filters.AddService<ValidationActionFilter>();
    options.Filters.AddService<PerformanceActionFilter>();
});
builder.Services.AddScoped<AuditActionFilter>();
builder.Services.AddScoped<ValidationActionFilter>();
builder.Services.AddScoped<PerformanceActionFilter>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/denied";
        options.Cookie.Name = "ApexPost.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(WebPolicies.BranchManager, policy => policy.RequireRole("BranchManager", "Administrator"));
    options.AddPolicy(WebPolicies.Administrator, policy => policy.RequireRole("Administrator"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("configured", policy =>
    {
        if (securityOptions.AllowedCorsOrigins.Length > 0)
        {
            policy.WithOrigins(securityOptions.AllowedCorsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("integration", limiterOptions =>
    {
        limiterOptions.PermitLimit = Math.Max(1, securityOptions.IntegrationRateLimitPermitLimit);
        limiterOptions.Window = TimeSpan.FromSeconds(Math.Max(1, securityOptions.IntegrationRateLimitWindowSeconds));
        limiterOptions.QueueLimit = 0;
        limiterOptions.AutoReplenishment = true;
    });
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit = Math.Max(1, securityOptions.LoginRateLimitPermitLimit);
        limiterOptions.Window = TimeSpan.FromSeconds(Math.Max(1, securityOptions.LoginRateLimitWindowSeconds));
        limiterOptions.QueueLimit = 0;
        limiterOptions.AutoReplenishment = true;
    });
});

builder.Services.AddInfrastructure(Path.Combine(builder.Environment.ContentRootPath, "data"));

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<MaintenanceModeMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseMiddleware<IpAllowlistMiddleware>();
app.UseCors("configured");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "ApexPost.Web"
})).AllowAnonymous();

app.MapGet("/integration/ping", () => Results.Ok(new
{
    status = "Accepted",
    service = "ApexPost.Web",
    correlationId = "available-in-response-header"
})).RequireRateLimiting("integration");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program;
