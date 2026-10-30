using System.Net;
using ApexPost.Web.Configuration;
using ApexPost.Web.Security;
using Microsoft.Extensions.Options;

namespace ApexPost.Web.Middleware;

public sealed class IpAllowlistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<WebSecurityOptions> _options;

    public IpAllowlistMiddleware(
        RequestDelegate next,
        IOptionsMonitor<WebSecurityOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        var isAdmin = path.StartsWithSegments("/admin");
        var isIntegration = path.StartsWithSegments("/integration");

        var requiresAllowlist =
            isAdmin ||
            isIntegration ||
            context.GetEndpoint()?.Metadata
                .GetMetadata<RequireAllowedIpAttribute>() is not null;

        if (!requiresAllowlist)
        {
            await _next(context);
            return;
        }

        var settings = _options.CurrentValue;

        var allowed = isAdmin
            ? settings.AdminAllowedIpAddresses
            : settings.IntegrationAllowedIpAddresses;

        var remoteIp = NormalizeIp(
            context.Connection.RemoteIpAddress);

        var isAllowed =
            remoteIp is not null &&
            allowed.Any(value =>
                IPAddress.TryParse(value, out var configured) &&
                NormalizeIp(configured)?.Equals(remoteIp) == true);

        if (!isAllowed)
        {
            context.Response.StatusCode =
                StatusCodes.Status403Forbidden;

            await context.Response.WriteAsJsonAsync(
                new
                {
                    title = "Forbidden",
                    status = 403
                },
                cancellationToken: context.RequestAborted);

            return;
        }

        await _next(context);
    }

    private static IPAddress? NormalizeIp(IPAddress? ip)
    {
        if (ip is null)
            return null;

        return ip.IsIPv4MappedToIPv6
            ? ip.MapToIPv4()
            : ip;
    }
}