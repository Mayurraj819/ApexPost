using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.RegularExpressions;

namespace ApexPost.Tests.Web;

public sealed class WebApplicationFactoryTests
{
    [Fact]
    public async Task SecurityHeadersAreReturned()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/health");

        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task MissingApiKeyReturns401()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["ApexPost:Security:IntegrationApiKeys:0"] = "phase6-test-key"
        });
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/integration/ping");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BlockedIpReturns403WhenApiKeyIsValid()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["ApexPost:Security:IntegrationApiKeys:0"] = "phase6-test-key",
            ["ApexPost:Security:IntegrationAllowedIpAddresses:0"] = "192.0.2.10"
        });
        using var client = CreateClient(factory);
        client.DefaultRequestHeaders.Add("X-API-Key", "phase6-test-key");

        using var response = await client.GetAsync("/integration/ping");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LoginRateLimitReturns429()
    {
        using var factory = CreateFactory(
            new Dictionary<string, string?>
            {
                ["ApexPost:Security:LoginRateLimitPermitLimit"] = "2",
                ["ApexPost:Security:LoginRateLimitWindowSeconds"] = "60"
            });

        using var client = CreateClient(factory);

        using var loginPage = await client.GetAsync("/account/login");
        loginPage.EnsureSuccessStatusCode();

        var html = await loginPage.Content.ReadAsStringAsync();

        var token = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\""
        ).Groups[1].Value;

        Assert.False(string.IsNullOrWhiteSpace(token));

        for (int i = 0; i < 3; i++)
        {
            using var response = await client.PostAsync(
                "/account/login",
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["__RequestVerificationToken"] = token,
                        ["Username"] = "invalid-user",
                        ["Password"] = "invalid-password"
                    }));

            Assert.Equal(
                i < 2
                    ? HttpStatusCode.OK
                    : HttpStatusCode.TooManyRequests,
                response.StatusCode);
        }
    }

    [Fact]
    public async Task UnauthenticatedAccessRedirectsToLogin()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/account/login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task DirectPasswordInConfigurationCanAuthenticate()
    {
        const string username = "phase6-clerk";
        const string password = "phase6-direct-config-password";
        var branchId = Guid.NewGuid().ToString();
        var staffId = Guid.NewGuid().ToString();
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["ApexPost:DemoAccounts:Accounts:0:Username"] = username,
            ["ApexPost:DemoAccounts:Accounts:0:DisplayName"] = "Phase 6 Clerk",
            ["ApexPost:DemoAccounts:Accounts:0:Role"] = "CounterClerk",
            ["ApexPost:DemoAccounts:Accounts:0:BranchId"] = branchId,
            ["ApexPost:DemoAccounts:Accounts:0:StaffId"] = staffId,
            ["ApexPost:DemoAccounts:Accounts:0:Password"] = password,
            ["ApexPost:Security:LoginRateLimitPermitLimit"] = "100"
        });
        using var client = CreateClient(factory);
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.21");
        var loginPage = await client.GetAsync("/account/login");
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.False(string.IsNullOrWhiteSpace(token));

        using var loginResponse = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Username"] = username,
            ["Password"] = password
        }));

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ClerkIsForbiddenFromVoiding()
    {
        const string passwordVariable = "APEXPOST_PHASE6_TEST_PASSWORD";
        const string password = "phase6-test-password";
        var previousPassword = Environment.GetEnvironmentVariable(passwordVariable);
        Environment.SetEnvironmentVariable(passwordVariable, password);
        try
        {
            var branchId = Guid.NewGuid().ToString();
            var staffId = Guid.NewGuid().ToString();
            using var factory = CreateFactory(new Dictionary<string, string?>
            {
                ["ApexPost:DemoAccounts:Accounts:0:Username"] = "phase6-clerk",
                ["ApexPost:DemoAccounts:Accounts:0:DisplayName"] = "Phase 6 Clerk",
                ["ApexPost:DemoAccounts:Accounts:0:Role"] = "CounterClerk",
                ["ApexPost:DemoAccounts:Accounts:0:BranchId"] = branchId,
                ["ApexPost:DemoAccounts:Accounts:0:StaffId"] = staffId,
                ["ApexPost:DemoAccounts:Accounts:0:PasswordEnvironmentVariable"] = passwordVariable
            });
            using var client = CreateClient(factory);
            var loginPage = await client.GetAsync("/account/login");
            var html = await loginPage.Content.ReadAsStringAsync();
            var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
            Assert.False(string.IsNullOrWhiteSpace(token));

            using var loginResponse = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Username"] = "phase6-clerk",
                ["Password"] = password
            }));
            Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

            using var response = await client.PostAsync("/Transactions/Void", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["transactionId"] = Guid.NewGuid().ToString(),
                ["reason"] = "Not authorized"
            }));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(passwordVariable, previousPassword);
        }
    }

    [Fact]
    public async Task CorrelationIdIsReturned()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        const string correlationId = "a0b5b9ec-1f68-4c3a-8f7d-dc1b0c8f0f10";
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        using var response = await client.GetAsync("/health");

        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").Single());
    }

    private static WebApplicationFactory<Program> CreateFactory(IReadOnlyDictionary<string, string?>? overrides = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(overrides ?? new Dictionary<string, string?>());
            });
        });

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
}
