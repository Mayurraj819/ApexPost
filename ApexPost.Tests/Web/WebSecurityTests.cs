using System.Net;
using System.Text;
using ApexPost.Web.Configuration;
using ApexPost.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ApexPost.Tests.Web;

public sealed class WebSecurityTests
{
    [Fact]
    public void ApiKeyComparison_IsCorrectAndDoesNotExposeConfiguredValue()
    {
        Assert.True(ApiKeyMiddleware.ConstantTimeEquals("integration-secret", "integration-secret"));
        Assert.False(ApiKeyMiddleware.ConstantTimeEquals("integration-secret", "wrong-secret"));
    }

    [Fact]
    public async Task CorrelationMiddleware_PropagatesValidCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "3f5c1c31-5cc0-4bbf-b4d4-5e1f37dd1c29";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        Assert.Equal("3f5c1c31-5cc0-4bbf-b4d4-5e1f37dd1c29", context.Items[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task MaintenanceMiddleware_ExemptsHealthAndRejectsOtherRequests()
    {
        var options = new StaticOptionsMonitor<WebSecurityOptions>(new WebSecurityOptions { MaintenanceMode = true });
        var middleware = new MaintenanceModeMiddleware(_ => Task.CompletedTask, options);
        var health = new DefaultHttpContext();
        health.Request.Path = "/health";
        var application = new DefaultHttpContext();
        application.Request.Path = "/dashboard";

        await middleware.InvokeAsync(health);
        await middleware.InvokeAsync(application);

        Assert.Equal(StatusCodes.Status200OK, health.Response.StatusCode);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, application.Response.StatusCode);
    }

    [Fact]
    public async Task IpAllowlistMiddleware_RejectsUnlistedAddress()
    {
        var options = new StaticOptionsMonitor<WebSecurityOptions>(new WebSecurityOptions
        {
            IntegrationAllowedIpAddresses = ["127.0.0.1"]
        });
        var middleware = new IpAllowlistMiddleware(_ => Task.CompletedTask, options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/integration/ping";
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_AddsBrowserSecurityHeaders()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, Options.Create(new WebSecurityOptions()));
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
        Assert.Contains("frame-ancestors 'none'", context.Response.Headers["Content-Security-Policy"].ToString());
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T> where T : class
    {
        public T CurrentValue { get; } = value;
        public T Get(string? name) => CurrentValue;
        public IDisposable OnChange(Action<T, string?> listener) => NoopDisposable.Instance;

        private sealed class NoopDisposable : IDisposable
        {
            public static readonly NoopDisposable Instance = new();
            public void Dispose() { }
        }
    }
}
