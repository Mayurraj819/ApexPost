using System.Net;
using Microsoft.Extensions.Options;
using ApexPost.Web.Configuration;

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
        if (!context.Request.Path.StartsWithSegments("/integration"))
        {
            await _next(context);
            return;
        }

        var allowed = _options.CurrentValue.IntegrationAllowedIpAddresses;
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
