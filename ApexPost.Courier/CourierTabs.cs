using ApexPost.Courier.Offline.Models;

namespace ApexPost.Courier;
public sealed class CourierTabs : TabbedPage
{
    private readonly CourierSession _session;
    private readonly Label _banner = new()
    {
        TextColor = Colors.DarkOrange
    };
    private readonly Label _identity = new();
    private readonly Label _pending = new();
    private readonly Label _status = new();
    private readonly VerticalStackLayout _scanList = new()
    {
        Spacing = 12
    };
    private readonly ContentPage _login;
    public CourierTabs(CourierSession session)
    {
        _session = session;
        Title = "ApexPost Courier";
        var token = new Entry
        {
            Placeholder = "Paste a development Courier JWT",
            IsPassword = true
        };
        _login = Page("Login", new Label { Text = "Fictional training data only. Token is kept in SecureStorage." }, _identity, token, Button("Sign in", async () =>
        {
            var value = token.Text ?? "";
            token.Text = "";
            await session.LoginAsync(value);
            CurrentPage = Children[1];
        }), Button("Sign out", session.LogoutAsync));
        var tracking = new Entry
        {
            Placeholder = "APX-12345678"
        };
        var location = new Entry
        {
            Placeholder = "Location"
        };
        var notes = new Editor
        {
            Placeholder = "Optional notes",
            AutoSize = EditorAutoSizeOption.TextChanges
        };
        var type = new Picker
        {
            Title = "Scan type",
            ItemsSource = Enum.GetNames<ScanType>().ToList(),
            SelectedIndex = 0
        };
        var offline = new Switch();
        offline.Toggled += (_, e) => session.SetOffline(e.Value);
        Children.Add(_login);
        Children.Add(Page("New Scan", _banner, tracking, type, location, notes, new Label { Text = "Work offline (Windows/local demo)" }, offline, Button("Save locally", () =>
        {
            session.Add((tracking.Text ?? "").Trim(), Enum.Parse<ScanType>((string)type.SelectedItem), location.Text ?? "", notes.Text ?? "");
            notes.Text = "";
            return Task.CompletedTask;
        })));
        Children.Add(Page("Pending Scans", _pending, Button("Sync now", () =>
        {
            session.Wake();
            return Task.CompletedTask;
        }), Button("Retry failed scans", () =>
        {
            session.RequeueFailed();
            return Task.CompletedTask;
        }), _scanList));
        Children.Add(Page("Sync Status", _status, new Label { Text = "Automatic sync runs after reconnecting and every 15 seconds while online. Network failures retain Pending scans." }));
        session.Changed += Refresh;
        Unloaded += (_, _) => session.Changed -= Refresh;
        Loaded += (_, _) => Refresh();
    }

    private ContentPage Page(string title, params View[] content) => new()
    {
        Title = title,
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = title,
                        FontSize = 24
                    }
                }
            }.With(content)
        }
    };
    private Button Button(string label, Func<Task> action)
    {
        var button = new Button
        {
            Text = label
        };
        button.Clicked += async (_, _) =>
        {
            button.IsEnabled = false;
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("ApexPost", ex is ArgumentException or InvalidOperationException or Integration.CourierApiException or Integration.CourierProtocolException ? ex.Message : "The operation could not finish. Check the server and sign in again.", "OK");
            }
            finally
            {
                button.IsEnabled = true;
                Refresh();
            }
        };
        return button;
    }

    private void Refresh() => MainThread.BeginInvokeOnMainThread(() =>
    {
        _identity.Text = _session.LoggedIn ? $"Courier: {_session.CourierId}" : "Not signed in";
        _banner.Text = _session.State.ServerUnavailable ? "Server unavailable – working offline" : "";
        _banner.IsVisible = _session.State.ServerUnavailable;
        var scans = _session.Scans();
        _pending.Text = $"Pending scans: {scans.Count(s => s.SyncStatus == SyncStatus.Pending)}";
        _status.Text = $"Circuit: {_session.State.Circuit}\nLast sync UTC: {_session.State.LastSyncUtc:O}\nFailures: {_session.State.Failures}\n{_session.State.Message}";
        _scanList.Children.Clear();
        foreach (var scan in scans.OrderByDescending(s => s.ScannedAtUtc))
            _scanList.Children.Add(new Label { Text = $"{scan.TrackingNumber} | {scan.ScanType} | {scan.SyncStatus}\n{scan.Location} | {scan.ScannedAtUtc:O}" });
        if (!_session.LoggedIn)
            CurrentPage = _login;
    });
}

internal static class LayoutExtensions
{
    internal static VerticalStackLayout With(this VerticalStackLayout layout, IEnumerable<View> views)
    {
        foreach (var v in views)
            layout.Children.Add(v);
        return layout;
    }
}
