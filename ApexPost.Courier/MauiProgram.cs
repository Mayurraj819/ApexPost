using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ApexPost.Courier.Integration;
using ApexPost.Courier.Offline.Abstractions;
using ApexPost.Courier.Offline.Persistence;

namespace ApexPost.Courier;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder().UseMauiApp<App>();
        using var stream = typeof(MauiProgram).Assembly.GetManifestResourceStream("ApexPost.Courier.appsettings.json") ?? throw new InvalidOperationException("Embedded endpoint configuration is missing.");
        using var config = JsonDocument.Parse(stream);
        var platform = DeviceInfo.Platform == DevicePlatform.Android ? "AndroidEmulator" : "Windows";
        var address = new Uri(config.RootElement.GetProperty("Api").GetProperty(platform).GetString()!);
        builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
        // Registers AddHttpClient<ApexApiClient> on this builder's service collection.
        builder.Services.AddCourierApi(address);
        builder.Services.AddSingleton(_ => new CourierDatabase(Path.Combine(FileSystem.AppDataDirectory, "CourierOffline")));
        builder.Services.AddSingleton<IScanRepository, CouchbaseScanRepository>();
        builder.Services.AddSingleton<IShipmentUploader>(s => s.GetRequiredService<ApexApiClient>());
        builder.Services.AddSingleton<CourierSession>();
        builder.Services.AddSingleton<CourierBootstrap>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}

public sealed class App(CourierBootstrap bootstrap) : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var loading = new ContentPage
        {
            Content = new Label
            {
                Text = "Opening ApexPost Courier...",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };
        var window = new Window(loading);
        loading.Loaded += async (_, _) =>
        {
            if (loading.BindingContext is not null)
                return;
            loading.BindingContext = bootstrap;
            try
            {
                window.Page = await bootstrap.OpenAsync();
            }
            catch (Exception ex)
            {
                loading.Content = new Label
                {
                    Text = $"Courier startup failed ({ex.GetType().Name}). Check app configuration and installed MAUI/Couchbase support.",
                    Margin = 24
                };
            }
        };
        window.Destroying += async (_, _) => await bootstrap.DisposeAsync();
        return window;
    }
}
