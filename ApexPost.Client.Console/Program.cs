using ApexPost.Client;
using Microsoft.Extensions.DependencyInjection;

var baseUrl = Environment.GetEnvironmentVariable("APEXPOST_API_BASE_URL") ?? "https://localhost:7268/";
var token=Environment.GetEnvironmentVariable("APEXPOST_ACCESS_TOKEN");
if(string.IsNullOrWhiteSpace(token)){Console.Error.WriteLine("Set APEXPOST_ACCESS_TOKEN to a development Courier JWT; see PARTD-START-HERE.md.");return;}
var services = new ServiceCollection();
services.AddHttpClient<IApexPostClient, ApexPostClient>(http => {http.BaseAddress=new Uri(baseUrl);http.DefaultRequestHeaders.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",token);});
await using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IApexPostClient>();
var tracking = args.FirstOrDefault() ?? $"APX-{Random.Shared.Next(10000000, 99999999)}";
try
{
    var created = await client.CreateShipmentV2Async(new CreateShipmentRequest { TrackingNumber = tracking, WeightKg = 1.25, RecipientName = "Fictional recipient", RecipientPhone = "+919876543210", Address = "12 Demo Street", City = "Pune" });
    Console.WriteLine($"Created: {created.TrackingNumber}");
    var fetched = await client.GetShipmentV2Async(tracking);
    Console.WriteLine($"Fetched: {fetched.Recipient.Name}, {fetched.Status}");
    var result = await client.UploadShipmentScansV2Async(tracking, new[] { new ScanUploadRequest { Id = "scan::" + Guid.NewGuid(), TrackingNumber = tracking, ScanType = "Pickup", CourierId = "COURIER-01", Location = "Demo depot", ScannedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2), Notes = "Fictional pickup" }, new ScanUploadRequest { Id = "scan::" + Guid.NewGuid(), TrackingNumber = tracking, ScanType = "InTransit", CourierId = "COURIER-01", Location = "Demo hub", ScannedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1), Notes = "Fictional transit" } });
    Console.WriteLine($"Scans: {result.Accepted} accepted, {result.DuplicatesSkipped} duplicates, {result.Rejected} rejected.");
    await client.GetShipmentV2Async("APX-00000000");
}
catch (ApiException<ProblemDetails> ex)when (ex.StatusCode == 404)
{
    Console.WriteLine($"Expected generated 404 exception: {ex.Result.Title} — {ex.Result.Detail}");
}
catch (ApiException ex)
{
    Console.Error.WriteLine($"API error {ex.StatusCode}: {ex.Message}");
    Environment.ExitCode = 1;
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Connection failed: {ex.Message}");
    Environment.ExitCode = 1;
}
