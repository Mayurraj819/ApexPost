using ApexPost.Courier.Integration;

namespace ApexPost.Courier;
public sealed class CourierBootstrap(CourierSession session) : IAsyncDisposable
{
    public async Task<Page> OpenAsync()
    {
        await session.RestoreAsync();
        return new CourierTabs(session);
    }

    public ValueTask DisposeAsync() => session.DisposeAsync();
}

public sealed class SecureTokenStore : ITokenStore
{
    private const string TokenKey = "apexpost.courier.accessToken";
    public event Action? Cleared;
    public Task<string?> GetAsync() => SecureStorage.Default.GetAsync(TokenKey);
    public Task SetAsync(string token) => SecureStorage.Default.SetAsync(TokenKey, token);
    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(TokenKey);
        Cleared?.Invoke();
        return Task.CompletedTask;
    }
}
