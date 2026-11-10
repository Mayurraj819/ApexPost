using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using ApexPost.Api.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ApexPost.Api.Security;
internal static class ApiSecurity
{
    internal static void AddCourierSecurity(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<CourierOwnershipFilter>();
        services.AddSingleton<DevelopmentTokens>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = config["Jwt:Issuer"] ?? "ApexPostDevelopment",
                ValidateAudience = true,
                ValidAudience = config["Jwt:Audience"] ?? "ApexPostCourier",
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SigningKey(config),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.FromSeconds(30),
                RoleClaimType = "role",
                NameClaimType = "sub"
            };
            options.Events = new JwtBearerEvents
            {
                OnChallenge = async c =>
                {
                    c.HandleResponse();
                    c.Response.Headers.WWWAuthenticate = "Bearer";
                    await Problem(c.HttpContext, 401, "A valid, unexpired bearer token is required.");
                },
                OnForbidden = c => Problem(c.HttpContext, 403, "You do not have permission for this operation.")
            };
        }).AddScheme<AuthenticationSchemeOptions, PartnerKeyHandler>("PartnerKey", _ =>
        {
        });
        services.AddAuthorization();
    }

    internal static SymmetricSecurityKey SigningKey(IConfiguration config)
    {
        var bytes = Convert.FromBase64String(config["Jwt:SigningKey"] ?? throw new InvalidOperationException("Configure Jwt:SigningKey with the Part D setup script."));
        if (bytes.Length < 32)
            throw new InvalidOperationException("JWT signing key requires at least 32 random bytes.");
        return new(bytes);
    }

    internal static bool EqualSecret(string a, string b) => CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(a)), SHA256.HashData(Encoding.UTF8.GetBytes(b)));
    internal static Task Problem(HttpContext c, int status, string detail)
    {
        c.Response.StatusCode = status;
        return c.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext { HttpContext = c, ProblemDetails = new ProblemDetails { Status = status, Title = Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(status), Detail = detail, Instance = c.Request.Path } }).AsTask();
    }
}

internal sealed class PartnerKeyHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IConfiguration config) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headers = Request.Headers["X-Api-Key"];
        var keys = config.GetSection("Partners:ApiKeys").Get<string[]>() ?? [];
        if (headers.Count != 1 || string.IsNullOrWhiteSpace(headers[0]) || !keys.Any(k => !string.IsNullOrWhiteSpace(k) && ApiSecurity.EqualSecret(headers[0]!, k)))
            return Task.FromResult(AuthenticateResult.Fail("Invalid partner key."));
        var identity = new ClaimsIdentity([new Claim("sub", "partner"), new Claim("role", "Partner")], Scheme.Name, "sub", "role");
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties p)
    {
        Response.Headers.WWWAuthenticate = "ApiKey";
        return ApiSecurity.Problem(Context, 401, "A valid partner X-Api-Key is required.");
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties p) => ApiSecurity.Problem(Context, 403, "Partner access is read-only.");
}

internal sealed class CourierOwnershipFilter : IAsyncActionFilter
{
    public CourierOwnershipFilter()
    {
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext c, ActionExecutionDelegate next)
    {
        var owner = c.HttpContext.User.FindFirstValue("courierId");
        if (string.IsNullOrWhiteSpace(owner) || c.ActionArguments.Values.OfType<List<ScanUploadRequest>>().Any(scans => scans.Any(s => s is not null && !string.Equals(s.CourierId, owner, StringComparison.Ordinal))))
        {
            c.Result = new ForbidResult();
            return;
        }

        await next();
    }
}

internal sealed record DevTokenRequest(string CourierId, string Role = "Courier");
internal sealed record DevTokenResponse(string AccessToken, DateTimeOffset ExpiresAtUtc);
internal sealed class DevelopmentTokens(IConfiguration config)
{
    internal IResult Issue(DevTokenRequest r, HttpContext c)
    {
        if (c.Connection.RemoteIpAddress is not { } ip || !IPAddress.IsLoopback(ip))
            return Results.Problem(statusCode: 403, detail: "Development tokens are available only on the local host.");
        var key = config["DevelopmentTokens:BootstrapKey"];
        var header = c.Request.Headers["X-Development-Key"];
        if (string.IsNullOrWhiteSpace(key) || header.Count != 1 || !ApiSecurity.EqualSecret(header[0] ?? "", key))
            return Results.Problem(statusCode: 401, detail: "A development bootstrap key is required.");
        if (string.IsNullOrWhiteSpace(r.CourierId) || r.CourierId.Length > 100 || r.Role is not ("Courier" or "Admin"))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["token"] = ["Provide a fictional courierId and Courier or Admin role."] });
        var expires = DateTimeOffset.UtcNow.AddMinutes(15);
        var jwt = new JwtSecurityToken(config["Jwt:Issuer"] ?? "ApexPostDevelopment", config["Jwt:Audience"] ?? "ApexPostCourier", [new Claim("sub", r.CourierId), new Claim("courierId", r.CourierId), new Claim("role", r.Role), new Claim("jti", Guid.NewGuid().ToString("N"))], DateTime.UtcNow, expires.UtcDateTime, new SigningCredentials(ApiSecurity.SigningKey(config), SecurityAlgorithms.HmacSha256));
        c.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new DevTokenResponse(new JwtSecurityTokenHandler().WriteToken(jwt), expires));
    }
}
