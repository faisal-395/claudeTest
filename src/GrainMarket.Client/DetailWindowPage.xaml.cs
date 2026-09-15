using Microsoft.AspNetCore.Components.WebView.Maui;

namespace GrainMarket.Client;

/// <summary>Hosts a single record's detail page in its own native window — a completely separate
/// BlazorWebView instance from the main window's, rooted directly at <see cref="DetailHost"/>
/// instead of the app's Router/MainLayout, so it shows just the document (no sidebar) with its own
/// Back button. Services registered as Singleton in MauiProgram (AuthState, ApiClient, …) are still
/// the exact same instances here, since every BlazorWebView in the app resolves from the one MAUI
/// service provider — only the routing/layout is separate.</summary>
public partial class DetailWindowPage : ContentPage
{
    public event EventHandler? CloseRequested;

    public DetailWindowPage(string recordType, int recordId)
    {
        InitializeComponent();

        blazorWebView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(DetailHost),
            Parameters = new Dictionary<string, object?>
            {
                [nameof(DetailHost.RecordType)] = recordType,
                [nameof(DetailHost.RecordId)] = recordId,
                [nameof(DetailHost.OnClose)] = (Action)(() => CloseRequested?.Invoke(this, EventArgs.Empty))
            }
        });
    }
}
