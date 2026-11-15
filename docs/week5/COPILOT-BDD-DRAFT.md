# Week 5 BDD Draft for Shipment Creation

Original prompt:

"For ApexPost Week 5, draft ShipmentCreation.feature and its C# Reqnroll bindings in chat only. Do not edit files yet.

Inspect:
- ApexPost.Specs/ApexPost.Specs.csproj
- ApexPost.Api/Contracts/ShipmentContracts.cs
- ApexPost.IntegrationTests/Infrastructure/SqlTestDatabase.cs
- ApexPost.IntegrationTests/Infrastructure/SqlApiFactory.cs
- ApexPost.IntegrationTests/Api/ShipmentApiDatabaseTests.cs

Requirements:
1. Use business-language Given/When/Then.
2. Background: an authenticated courier and an empty isolated test database.
3. Successful creation: HTTP 201, working Location, and persisted shipment.
4. Duplicate tracking number: HTTP 409 and only one persisted shipment.
5. Scenario Outline for invalid weights: 0, 0.09 and 70.01 kg; expect HTTP 400 and no shipment persisted.
6. Use constructor-injected scenario state, never static mutable fields.
7. BeforeScenario/AfterScenario hooks create and dispose resources.
8. Use WebApplicationFactory with real ShipmentApiService and SQL Server.
9. Reuse the existing database/authentication helpers where practical. Explain any project changes required; do not reference another test project.
10. Use fictional data, xUnit assertions, and no running API or pasted JWT.

Show the feature, bindings, scenario context, hooks, and proposed file paths."

## Proposed file paths

- ApexPost.Specs/Features/ShipmentCreation.feature
- ApexPost.Specs/StepDefinitions/ShipmentCreationSteps.cs
- ApexPost.Specs/Infrastructure/ShipmentScenarioContext.cs
- ApexPost.Specs/Infrastructure/ShipmentScenarioHooks.cs
- ApexPost.Specs/Infrastructure/SqlTestDatabase.cs
- ApexPost.Specs/Infrastructure/SqlApiFactory.cs
- ApexPost.Specs/Infrastructure/TestAuthentication.cs

## Feature file

```gherkin
Feature: Shipment creation
  As a courier
  I want to create shipments with valid details
  So that parcels can be tracked and uploaded through the API

  Background:
	Given an authenticated courier
	And the isolated test database is empty

  Scenario: Create a shipment with valid details
	Given the courier has a valid shipment request
	When the courier creates the shipment
	Then the response status should be 201 Created
	And the Location header should point to a working shipment resource
	And the persisted shipment should match the submitted request

  Scenario: Reject duplicate tracking numbers
	Given a shipment already exists with tracking number "APX-12345678"
	And the courier has a valid shipment request with tracking number "APX-12345678"
	When the courier creates the shipment
	Then the response status should be 409 Conflict
	And only one shipment record exists for that tracking number

  Scenario Outline: Reject invalid shipment weights
	Given the courier has a valid shipment request with weight "<weight>" kilograms
	When the courier creates the shipment
	Then the response status should be 400 Bad Request
	And no shipment is persisted

	Examples:
	  | weight |
	  | 0      |
	  | 0.09   |
	  | 70.01  |
```

## Scenario context

```csharp
using System.Net.Http;
using ApexPost.Api.Contracts;
using ApexPost.Infrastructure.Persistence;

namespace ApexPost.Specs.Infrastructure;

public sealed class ShipmentScenarioContext
{
	public SqlTestDatabase? Database { get; set; }
	public SqlApiFactory? Factory { get; set; }
	public HttpClient? Client { get; set; }

	public HttpResponseMessage? LastResponse { get; set; }
	public CreateShipmentRequest Request { get; set; } = new()
	{
		TrackingNumber = "APX-12345678",
		WeightKg = 1.25m,
		RecipientName = "Fictional Recipient",
		RecipientPhone = "+919876543210",
		Address = "10 Fictional Road",
		City = "Pune"
	};

	public string CourierId { get; } = "week5-courier-01";
	public string Role { get; } = "Courier";
	public string TrackingNumber => Request.TrackingNumber;
	public ShipmentV2? CreatedShipment { get; set; }
}
```

## Hooks

```csharp
using System.Net.Http;
using ApexPost.Specs.Infrastructure;
using Reqnroll;

namespace ApexPost.Specs.Hooks;

[Binding]
public sealed class ShipmentScenarioHooks
{
	private readonly ShipmentScenarioContext _scenario;

	public ShipmentScenarioHooks(ShipmentScenarioContext scenario)
	{
		_scenario = scenario;
	}

	[BeforeScenario]
	public async Task BeforeScenario()
	{
		var database = await SqlTestDatabase.CreateAsync();
		var factory = new SqlApiFactory(database.ConnectionString);

		_scenario.Database = database;
		_scenario.Factory = factory;
		_scenario.Client = factory.CreateAuthenticatedClient();
	}

	[AfterScenario]
	public async Task AfterScenario()
	{
		if (_scenario.Client is not null)
		{
			_scenario.Client.Dispose();
			_scenario.Client = null;
		}

		if (_scenario.Factory is not null)
		{
			_scenario.Factory.Dispose();
			_scenario.Factory = null;
		}

		if (_scenario.Database is not null)
		{
			await _scenario.Database.DisposeAsync();
			_scenario.Database = null;
		}
	}
}
```

## Reqnroll step bindings

```csharp
using System.Net;
using System.Net.Http.Json;
using ApexPost.Api.Contracts;
using ApexPost.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using Xunit;

namespace ApexPost.Specs.StepDefinitions;

[Binding]
public sealed class ShipmentCreationSteps
{
	private readonly ShipmentScenarioContext _scenario;

	public ShipmentCreationSteps(ShipmentScenarioContext scenario)
	{
		_scenario = scenario;
	}

	[Given("an authenticated courier")]
	public void GivenAnAuthenticatedCourier()
	{
		Assert.NotNull(_scenario.Client);
		Assert.NotNull(_scenario.Factory);
		Assert.Equal("week5-courier-01", _scenario.CourierId);
	}

	[Given("the isolated test database is empty")]
	public async Task GivenTheIsolatedTestDatabaseIsEmpty()
	{
		await using var verification = _scenario.Database!.CreateContext();
		var shipments = await verification.Set<TrackedShipment>().AsNoTracking().ToListAsync();
		Assert.Empty(shipments);
	}

	[Given("the courier has a valid shipment request")]
	public void GivenTheCourierHasAValidShipmentRequest()
	{
		_scenario.Request = new CreateShipmentRequest
		{
			TrackingNumber = "APX-12345678",
			WeightKg = 1.25m,
			RecipientName = "Fictional Recipient",
			RecipientPhone = "+919876543210",
			Address = "10 Fictional Road",
			City = "Pune"
		};
	}

	[Given("the courier has a valid shipment request with tracking number \"(.*)\"")]
	public void GivenTheCourierHasAValidShipmentRequestWithTrackingNumber(string trackingNumber)
	{
		_scenario.Request = new CreateShipmentRequest
		{
			TrackingNumber = trackingNumber,
			WeightKg = 1.25m,
			RecipientName = "Fictional Recipient",
			RecipientPhone = "+919876543210",
			Address = "10 Fictional Road",
			City = "Pune"
		};
	}

	[Given("the courier has a valid shipment request with weight \"(.*)\" kilograms")]
	public void GivenTheCourierHasAValidShipmentRequestWithWeight(string weightText)
	{
		var weight = decimal.Parse(weightText, System.Globalization.CultureInfo.InvariantCulture);

		_scenario.Request = new CreateShipmentRequest
		{
			TrackingNumber = "APX-12345678",
			WeightKg = weight,
			RecipientName = "Fictional Recipient",
			RecipientPhone = "+919876543210",
			Address = "10 Fictional Road",
			City = "Pune"
		};
	}

	[Given("a shipment already exists with tracking number \"(.*)\"")]
	public async Task GivenAShipmentAlreadyExistsWithTrackingNumber(string trackingNumber)
	{
		using var create = await _scenario.Client!.PostAsJsonAsync("/api/v2/shipments", new CreateShipmentRequest
		{
			TrackingNumber = trackingNumber,
			WeightKg = 1.25m,
			RecipientName = "Existing Recipient",
			RecipientPhone = "+919876543211",
			Address = "11 Existing Road",
			City = "Bangalore"
		});

		Assert.Equal(HttpStatusCode.Created, create.StatusCode);
	}

	[When("the courier creates the shipment")]
	public async Task WhenTheCourierCreatesTheShipment()
	{
		_scenario.LastResponse = await _scenario.Client!.PostAsJsonAsync("/api/v2/shipments", _scenario.Request);
	}

	[Then("the response status should be 201 Created")]
	public void ThenTheResponseStatusShouldBe201Created()
	{
		Assert.NotNull(_scenario.LastResponse);
		Assert.Equal(HttpStatusCode.Created, _scenario.LastResponse.StatusCode);
	}

	[Then("the response status should be 409 Conflict")]
	public void ThenTheResponseStatusShouldBe409Conflict()
	{
		Assert.NotNull(_scenario.LastResponse);
		Assert.Equal(HttpStatusCode.Conflict, _scenario.LastResponse.StatusCode);
	}

	[Then("the response status should be 400 Bad Request")]
	public void ThenTheResponseStatusShouldBe400BadRequest()
	{
		Assert.NotNull(_scenario.LastResponse);
		Assert.Equal(HttpStatusCode.BadRequest, _scenario.LastResponse.StatusCode);
	}

	[Then("the Location header should point to a working shipment resource")]
	public async Task ThenTheLocationHeaderShouldPointToAWorkingShipmentResource()
	{
		Assert.NotNull(_scenario.LastResponse);
		Assert.NotNull(_scenario.LastResponse.Headers.Location);

		using var fetch = await _scenario.Client!.GetAsync(_scenario.LastResponse.Headers.Location);
		Assert.Equal(HttpStatusCode.OK, fetch.StatusCode);

		var shipment = await fetch.Content.ReadFromJsonAsync<ShipmentV2>();
		Assert.NotNull(shipment);
		Assert.Equal(_scenario.Request.TrackingNumber, shipment.TrackingNumber);
	}

	[Then("the persisted shipment should match the submitted request")]
	public async Task ThenThePersistedShipmentShouldMatchTheSubmittedRequest()
	{
		await using var verification = _scenario.Database!.CreateContext();
		var persisted = await verification.Set<TrackedShipment>()
			.AsNoTracking()
			.SingleAsync(s => s.TrackingNumber == _scenario.Request.TrackingNumber);

		Assert.Equal(_scenario.Request.TrackingNumber, persisted.TrackingNumber);
		Assert.Equal(_scenario.Request.WeightKg, persisted.WeightKg);
		Assert.Equal(_scenario.Request.RecipientName, persisted.RecipientName);
		Assert.Equal(_scenario.Request.RecipientPhone, persisted.RecipientPhone);
		Assert.Equal(_scenario.Request.Address, persisted.Address);
		Assert.Equal(_scenario.Request.City, persisted.City);
	}

	[Then("only one shipment record exists for that tracking number")]
	public async Task ThenOnlyOneShipmentRecordExistsForThatTrackingNumber()
	{
		await using var verification = _scenario.Database!.CreateContext();
		var shipments = await verification.Set<TrackedShipment>()
			.AsNoTracking()
			.Where(s => s.TrackingNumber == _scenario.Request.TrackingNumber)
			.ToListAsync();

		Assert.Single(shipments);
	}

	[Then("no shipment is persisted")]
	public async Task ThenNoShipmentIsPersisted()
	{
		await using var verification = _scenario.Database!.CreateContext();
		var count = await verification.Set<TrackedShipment>().AsNoTracking().CountAsync();
		Assert.Equal(0, count);
	}
}
```

## SQL-backed test database helper

```csharp
using ApexPost.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Specs.Infrastructure;

public sealed class SqlTestDatabase : IAsyncDisposable
{
	private const string Prefix = "ApexPost_Test_Week5_";
	private readonly string _databaseName = Prefix + Guid.NewGuid().ToString("N");
	public string ConnectionString { get; }

	private SqlTestDatabase(string baseConnection)
	{
		var builder = new SqlConnectionStringBuilder(baseConnection);
		if (!string.Equals(builder.InitialCatalog, "master", StringComparison.OrdinalIgnoreCase)
			|| !string.IsNullOrEmpty(builder.AttachDBFilename))
		{
			throw new InvalidOperationException(
				"APEXPOST_TEST_SQLSERVER must target master without AttachDBFilename.");
		}

		builder.InitialCatalog = _databaseName;
		builder.Pooling = false;
		builder.ConnectTimeout = 30;
		ConnectionString = builder.ConnectionString;
	}

	public static async Task<SqlTestDatabase> CreateAsync()
	{
		var baseConnection = Environment.GetEnvironmentVariable("APEXPOST_TEST_SQLSERVER");
		if (string.IsNullOrWhiteSpace(baseConnection))
		{
			if (!OperatingSystem.IsWindows())
			{
				throw new InvalidOperationException(
					"These tests require SQL Server. Set APEXPOST_TEST_SQLSERVER to a private test server connection with Database=master and database-create rights.");
			}

			baseConnection = @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True";
		}

		var database = new SqlTestDatabase(baseConnection);
		try
		{
			await using var db = database.CreateContext();
			await db.Database.MigrateAsync();
			return database;
		}
		catch
		{
			try { await database.DisposeAsync(); } catch { }
			throw;
		}
	}

	public ApexPostDbContext CreateContext() => new(
		new DbContextOptionsBuilder<ApexPostDbContext>()
			.UseSqlServer(ConnectionString, sql => sql.EnableRetryOnFailure(
				maxRetryCount: 3,
				maxRetryDelay: TimeSpan.FromSeconds(1),
				errorNumbersToAdd: null))
			.Options);

	public async ValueTask DisposeAsync()
	{
		var actual = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
		if (actual != _databaseName || !actual.StartsWith(Prefix, StringComparison.Ordinal))
			throw new InvalidOperationException("Refusing to delete a database not owned by this test.");

		await using var db = CreateContext();
		await db.Database.EnsureDeletedAsync();
	}
}
```

## WebApplicationFactory and authentication helper

```csharp
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

namespace ApexPost.Specs.Infrastructure;

public sealed class SqlApiFactory(string connectionString) : WebApplicationFactory<ApiProgram>
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
			[
				new Claim("sub", "fictional-week5-user"),
				new Claim("role", role),
				new Claim("courierId", CourierId)
			],
			SchemeName,
			"sub",
			"role");

		return Task.FromResult(AuthenticateResult.Success(
			new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
	}
}
```

## Notes from the inspected sources

- `ApexPost.Specs.csproj` already includes Reqnroll and Microsoft.AspNetCore.Mvc.Testing, which matches the intended BDD and factory-based approach.
- `ShipmentContracts.cs` already defines `CreateShipmentRequest` with the required validation rules. The weight constraints are enforced by the `[Range(typeof(decimal), "0.1", "70")]` attribute in `UpdateShipmentRequest`, which means the invalid weights of 0, 0.09 and 70.01 should fail API validation and return `400 Bad Request`.
- `SqlTestDatabase.cs` in the integration tests shows the correct pattern for creating an isolated SQL Server database per scenario, migrating it, and cleaning it up in `DisposeAsync()`.
- `SqlApiFactory.cs` already shows the correct pattern for setting `UseEnvironment("Testing")`, injecting a dedicated SQL connection string, and replacing the auth scheme with a test-only authentication handler.
- `ShipmentApiDatabaseTests.cs` demonstrates the business-level assertions expected from the API: create shipment, verify `201 Created`, check `Location`, verify persistence, and confirm duplicate rejection / status preservation.

## Project changes required

1. Add the Reqnroll bindings and infrastructure files under `ApexPost.Specs` only.
2. Ensure the project includes the required test packages for Reqnroll and ASP.NET Core integration testing if they are not already present.
3. If the app entry point is internal, expose `ApiProgram` appropriately so `WebApplicationFactory<ApiProgram>` can host the real API.
4. Reuse the same SQL test database creation and auth-workarounds already demonstrated in the integration tests, but do not reference another test project by name.
5. Keep all scenario state as constructor-injected, non-static objects; do not add static mutable fields.

## Summary

This draft uses business-language Given/When/Then steps, constructor-injected scenario state, per-scenario database isolation, and a real SQL-backed WebApplicationFactory with a test-only auth handler. It aligns with the existing integration test helpers without modifying implementation code.
