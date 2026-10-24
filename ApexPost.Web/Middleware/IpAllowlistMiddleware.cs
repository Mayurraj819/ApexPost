using System.Net;
using ApexPost.Web.Configuration;
using ApexPost.Web.Security;
using Microsoft.Extensions.Options;

namespace ApexPost.Web.Middleware;

public sealed class IpAllowlistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<WebSecurityOptions> _options;

    public IpAllowlistMiddleware(RequestDelegate next, IOptionsMonitor<WebSecurityOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var metadata = endpoint?.Metadata;
        var requiresAllowlist = context.Request.Path.StartsWithSegments("/integration") || context.Request.Path.StartsWithSegments("/admin") || metadata?.GetMetadata<RequireAllowedIpAttribute>() is not null;
        if (!requiresAllowlist)
        {
            await _next(context);
            return;
        }

        var allowed = context.Request.Path.StartsWithSegments("/admin")
            ? _options.CurrentValue.AdminAllowedIpAddresses
            : _options.CurrentValue.IntegrationAllowedIpAddresses;

        var remoteIp = context.Connection.RemoteIpAddress;
        var isAllowed = allowed.Length == 0 || (remoteIp is not null && allowed.Any(value => IPAddress.TryParse(value, out var configured) && configured.Equals(remoteIp)));
        if (!isAllowed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { title = "Forbidden", status = 403 });
            return;
        }

        await _next(context);
    }
}
