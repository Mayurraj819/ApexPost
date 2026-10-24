using System.Threading.RateLimiting;
using ApexPost.Infrastructure;
using ApexPost.Web.Configuration;
using ApexPost.Web.Filters;
using ApexPost.Web.Middleware;
using ApexPost.Web.Security;
using ApexPost.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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

builder.Services.AddScoped<IAuthorizationHandler, SameBranchAuthorizationHandler>();

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
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                var redirect = context.Request.PathBase + "/account/login?ReturnUrl=" + Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
                context.Response.Redirect(redirect);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(WebPolicies.BranchManager, policy => policy.RequireRole("BranchManager", "Administrator"));
    options.AddPolicy(WebPolicies.Administrator, policy => policy.RequireRole("Administrator"));
    options.AddPolicy(WebPolicies.SameBranch, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new SameBranchRequirement());
    });
    options.AddPolicy(WebPolicies.BranchManagerOnly, policy => policy.RequireRole("BranchManager", "Administrator"));
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
builder.Services.AddSingleton<IProductCatalogCache, ProductCatalogCache>();

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
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/integration") || context.Request.Path.StartsWithSegments("/admin"),
    branch =>
    {
        branch.UseMiddleware<ApiKeyMiddleware>();
        branch.UseMiddleware<IpAllowlistMiddleware>();
    });
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
})).WithMetadata(new RequireApiKeyAttribute()).RequireRateLimiting("integration");

app.MapGet("/admin/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "ApexPost.Web",
    scope = "admin"
})).WithMetadata(new RequireApiKeyAttribute()).WithMetadata(new RequireAllowedIpAttribute());

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program;
