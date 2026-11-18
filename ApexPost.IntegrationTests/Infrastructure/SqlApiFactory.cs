using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApexPost.IntegrationTests.Infrastructure;

// Test-only authentication replaces credential verification, not authorization or persistence.
internal sealed class SqlApiFactory(string connectionString) : WebApplicationFactory<ApiProgram>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ApexPost", connectionString);
        builder.UseSetting("Jwt:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:ApexPost"] = connectionString }));
        builder.ConfigureTestServices(services => services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthentication.SchemeName;
                options.DefaultChallengeScheme = TestAuthentication.SchemeName;
                options.DefaultForbidScheme = TestAuthentication.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthentication>(
                TestAuthentication.SchemeName, _ => { }));
    }

    public HttpClient CreateAuthenticatedClient(string role = "Courier")
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("X-Week5-Test-Role", role);
        return client;
    }
}

internal sealed class TestAuthentication(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Week5DatabaseTest";
    public const string CourierId = "week5-courier-01";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers["X-Week5-Test-Role"].ToString();
        if (role is not ("Courier" or "Admin"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(
            [new Claim("sub", "fictional-week5-user"), new Claim("role", role),
             new Claim("courierId", CourierId)], SchemeName, "sub", "role");
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
