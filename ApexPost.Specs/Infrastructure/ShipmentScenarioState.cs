using ApexPost.Api.Contracts;
using ApexPost.IntegrationTests.Infrastructure;

namespace ApexPost.Specs.Infrastructure;

// Copilot-assisted creation draft, reviewed and extended; see docs/week5/COPILOT-BDD-REVIEW.md.
// Reqnroll creates one injected state object per scenario. Hooks own its resources.
public sealed class ShipmentScenarioState
{
    private SqlTestDatabase? _database;
    private SqlApiFactory? _factory;
    private HttpClient? _client;
    private HttpResponseMessage? _response;

    internal SqlTestDatabase Database => _database ?? throw new InvalidOperationException("Scenario database was not initialized.");
    public HttpClient Client => _client ?? throw new InvalidOperationException("Scenario client was not initialized.");
    public HttpResponseMessage Response => _response ?? throw new InvalidOperationException("No scenario action has produced a response.");
    public CreateShipmentRequest Request { get; set; } = NewRequest();
    public CreateShipmentRequest? ExistingRequest { get; set; }
    public ScanUploadRequest[] Scans { get; set; } = [];
    public ScanUploadResponse? UploadResult { get; set; }

    public static CreateShipmentRequest NewRequest(string tracking = "APX-12345678") => new()
    {
        TrackingNumber = tracking, WeightKg = 1.25m, RecipientName = "Fictional Recipient",
        RecipientPhone = "+919876543210", Address = "10 Fictional Road", City = "Pune"
    };

    internal async Task StartAsync()
    {
        try
        {
            _database = await SqlTestDatabase.CreateAsync();
            _factory = new SqlApiFactory(_database.ConnectionString);
            _client = _factory.CreateAuthenticatedClient();
        }
        catch
        {
            try { await StopAsync(); }
            catch { /* Keep the original initialization error. */ }
            throw;
        }
    }

    public void SetResponse(HttpResponseMessage response)
    {
        _response?.Dispose();
        _response = response;
        UploadResult = null;
    }

    internal async Task StopAsync()
    {
        try
        {
            _response?.Dispose();
            _client?.Dispose();
        }
        finally
        {
            _response = null;
            _client = null;
            try
            {
                if (_factory is not null)
                    await _factory.DisposeAsync();
            }
            finally
            {
                _factory = null;
                if (_database is { } database)
                {
                    _database = null;
                    await database.DisposeAsync();
                }
            }
        }
    }
}
