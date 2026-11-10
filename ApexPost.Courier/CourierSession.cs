using System.Text.Json;
using System.Text.RegularExpressions;
using ApexPost.Courier.Integration;
using ApexPost.Courier.Offline.Abstractions;
using ApexPost.Courier.Offline.Models;
using ApexPost.Courier.Offline.Persistence;
using Microsoft.Extensions.Logging;

namespace ApexPost.Courier;
public sealed class CourierSession : IAsyncDisposable
{
    private readonly ITokenStore _tokens;
    private readonly ApexApiClient _api;
    private readonly IScanRepository _repository;
    private readonly CourierDatabase _database;
    private readonly CourierSync _sync;
    private readonly ILogger<CourierSession> _logger;
    private readonly CancellationTokenSource _stop = new();
    private readonly SemaphoreSlim _signal = new(0, 1);
    private readonly Task _worker;
    private bool _disposed;
    public CourierState State { get; }
    public string CourierId { get; private set; } = "";
    public bool LoggedIn { get; private set; }
    public bool WorkOffline { get; private set; }

    public event Action? Changed;
    public CourierSession(ITokenStore tokens, ApexApiClient api, IScanRepository repository, CourierDatabase database, CourierState state, ILogger<CourierSession> logger)
    {
        _tokens = tokens;
        _api = api;
        _repository = repository;
        _database = database;
        State = state;
        _logger = logger;
        _sync = new(repository, api, state);
        tokens.Cleared += SignedOut;
        state.Changed += Notify;
        Connectivity.Current.ConnectivityChanged += NetworkChanged;
        database.StartChangeListener(message =>
        {
            logger.LogInformation("Local courier change: {Summary}", message);
            Notify();
        });
        _worker = Task.Run(WorkerAsync);
    }

    public IReadOnlyList<ShipmentScan> Scans() => _disposed ? [] : _repository.ListAll().Where(s => s.CourierId == CourierId).ToArray();
    public async Task RestoreAsync()
    {
        var token = await _tokens.GetAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            try
            {
                CourierId = ReadCourier(token);
                LoggedIn = true;
                State.Report("Stored session loaded; server validates it on the next request.");
                Wake();
            }
            catch (ArgumentException)
            {
                await _tokens.ClearAsync();
            }
        }
    }

    public async Task LoginAsync(string token)
    {
        var courier = ReadCourier(token);
        await _tokens.SetAsync(token);
        try
        {
            await _api.ValidateLoginAsync(_stop.Token);
            CourierId = courier;
            LoggedIn = true;
            State.Report("Signed in.");
            Wake();
            Notify();
        }
        catch
        {
            await _tokens.ClearAsync();
            throw;
        }
    }

    private static string ReadCourier(string token)
    {
        // Decode for display/filtering only; authentication is the server's responsibility.
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
                throw new ArgumentException("Paste a valid Courier JWT.");
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
            using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            var id = doc.RootElement.GetProperty("courierId").GetString();
            var role = doc.RootElement.GetProperty("role");
            var courier = role.ValueKind == JsonValueKind.Array ? role.EnumerateArray().Any(r => r.GetString() == "Courier") : role.GetString() == "Courier";
            if (string.IsNullOrWhiteSpace(id) || !courier)
                throw new ArgumentException("The app requires a Courier token with courierId.");
            return id;
        }
        catch (Exception ex)when (ex is FormatException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new ArgumentException("The token payload is invalid.");
        }
    }

    public Task LogoutAsync() => _tokens.ClearAsync();
    private void SignedOut()
    {
        LoggedIn = false;
        CourierId = "";
        State.Report("Sign in to continue. Local scans are retained.");
        Notify();
    }

    public void SetOffline(bool value)
    {
        WorkOffline = value;
        State.Report(value ? "Working offline; scans are saved locally." : "Online mode enabled.");
        Wake();
        Notify();
    }

    public void Add(string tracking, ScanType type, string location, string notes)
    {
        if (!LoggedIn)
            throw new InvalidOperationException("Sign in before recording scans.");
        if (!Regex.IsMatch(tracking, @"\AAPX-[0-9]{8}\z"))
            throw new ArgumentException("Tracking number must be APX- followed by eight digits.");
        if (string.IsNullOrWhiteSpace(location) || location.Length > 200 || notes.Length > 2000)
            throw new ArgumentException("Provide a location up to 200 characters and notes up to 2000 characters.");
        _repository.Create(new ShipmentScan { TrackingNumber = tracking, CourierId = CourierId, ScanType = type, Location = location, Notes = notes });
        State.Report("Scan saved locally first.");
        Wake();
        Notify();
    }

    public void RequeueFailed()
    {
        foreach (var scan in Scans().Where(s => s.SyncStatus == SyncStatus.Failed))
            _repository.Update(scan with { SyncStatus = SyncStatus.Pending });
        Wake();
    }

    public void Wake()
    {
        if (!_disposed && _signal.CurrentCount == 0)
        {
            try
            {
                _signal.Release();
            }
            catch (SemaphoreFullException)
            {
            }
        }
    }

    private void NetworkChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
            Wake();
        Notify();
    }

    private async Task WorkerAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                await _signal.WaitAsync(TimeSpan.FromSeconds(15), _stop.Token);
                if (LoggedIn && !WorkOffline && Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                {
                    try
                    {
                        await _sync.SyncAsync(CourierId, _stop.Token);
                    }
                    catch (OperationCanceledException)when (_stop.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Courier sync stopped with {ErrorType}", ex.GetType().Name);
                        State.Report("Sync could not finish. Local scans were retained.", true);
                    }
                }
            }
        }
        catch (OperationCanceledException)when (_stop.IsCancellationRequested)
        {
        }
    }

    private void Notify() => Changed?.Invoke();
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;
        _tokens.Cleared -= SignedOut;
        State.Changed -= Notify;
        Connectivity.Current.ConnectivityChanged -= NetworkChanged;
        _stop.Cancel();
        await _worker;
        _sync.Dispose();
        _database.Dispose();
        _signal.Dispose();
        _stop.Dispose();
    }
}
