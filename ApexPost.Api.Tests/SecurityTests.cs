using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ApexPost.Api.Tests;
public sealed class TransportAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim("role", "Courier"), new Claim("role", "Admin"), new Claim("courierId", "COURIER-01")], Scheme.Name, "courierId", "role")), Scheme.Name)));
}

internal static class SecurityTokens
{
    internal const string Issuer = "ApexPostTests", Audience = "ApexPostTestsClient";
    internal static readonly byte[] Key = RandomNumberGenerator.GetBytes(64);
    internal static readonly string PartnerKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    internal static string Create(string role = "Courier", bool expired = false, string? issuer = null, string? audience = null, bool wrongKey = false)
    {
        var jwt = new JwtSecurityToken(issuer ?? Issuer, audience ?? Audience, [new Claim("role", role), new Claim("courierId", "COURIER-01")], DateTime.UtcNow.AddHours(-1), expired ? DateTime.UtcNow.AddMinutes(-2) : DateTime.UtcNow.AddMinutes(10), new SigningCredentials(new SymmetricSecurityKey(wrongKey ? RandomNumberGenerator.GetBytes(64) : Key), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}

public sealed class SecurityTests
{
    [Theory]
    [InlineData("missing")]
    [InlineData("expired")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("signature")]
    public async Task Invalid_tokens_are_401_ProblemDetails(string kind)
    {
        await using var f = new ApiFactory(authenticate: false);
        using var c = f.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        if (kind != "missing")
            c.DefaultRequestHeaders.Authorization = new("Bearer", SecurityTokens.Create(expired: kind == "expired", issuer: kind == "issuer" ? "wrong" : null, audience: kind == "audience" ? "wrong" : null, wrongKey: kind == "signature"));
        var r = await c.GetAsync("/api/v2/shipments");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Roles_ownership_and_aliases_are_enforced()
    {
        await using var f = new ApiFactory(authenticate: false);
        using var c = f.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        foreach (var path in new[]
        {
            "/api/shipments",
            "/api/v1/shipments",
            "/api/v2/shipments"
        }

        )
            Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync(path)).StatusCode);
        c.DefaultRequestHeaders.Authorization = new("Bearer", SecurityTokens.Create());
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/v2/shipments", TransportTests.Create())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.DeleteAsync("/api/v2/shipments/APX-12345678")).StatusCode);
        var scan = new
        {
            id = "scan::" + Guid.NewGuid(),
            trackingNumber = "APX-12345678",
            scanType = "Pickup",
            courierId = "OTHER",
            location = "Demo",
            scannedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            notes = ""
        };
        var forbidden = await c.PostAsJsonAsync("/api/v2/shipments/APX-12345678/scans", new[] { scan });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal("application/problem+json", forbidden.Content.Headers.ContentType?.MediaType);
        c.DefaultRequestHeaders.Authorization = new("Bearer", SecurityTokens.Create("Admin"));
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PatchAsJsonAsync("/api/v2/shipments/APX-12345678/status", new { status = "PickedUp" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync("/api/v2/shipments/APX-12345678")).StatusCode);
    }

    [Fact]
    public async Task Partner_key_is_read_only_and_required()
    {
        await using var f = new ApiFactory(authenticate: false);
        using var c = f.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        c.DefaultRequestHeaders.Authorization = new("Bearer", SecurityTokens.Create());
        await c.PostAsJsonAsync("/api/v2/shipments", TransportTests.Create());
        c.DefaultRequestHeaders.Authorization = null;
        const string path = "/api/v2/partner/shipments/APX-12345678";
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync(path)).StatusCode);
        c.DefaultRequestHeaders.Add("X-Api-Key", "wrong");
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync(path)).StatusCode);
        c.DefaultRequestHeaders.Remove("X-Api-Key");
        c.DefaultRequestHeaders.Add("X-Api-Key", SecurityTokens.PartnerKey);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v2/shipments")).StatusCode);
    }

    [Fact]
    public async Task Production_has_no_development_token_endpoint()
    {
        await using var f = new ApiFactory("Production", authenticate: false);
        using var c = f.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync("/dev/token", new { courierId = "DEMO", role = "Admin" })).StatusCode);
    }
}
