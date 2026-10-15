using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Web.WebView2.Core;

namespace GrainMarket.Client;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        blazorWebView.BlazorWebViewInitializing += OnBlazorWebViewInitializing;
    }

    // Reported bug: the SearchSelect dropdown's scrollable menu (position:absolute, nested inside
    // the page's own scrollable .main-content) scrolls fine for a moment and then randomly stops
    // responding to the mouse wheel mid-gesture. CSS-only attempts (overscroll-behavior: contain,
    // overflow-anchor: none, forcing a GPU layer) didn't resolve it, which points at Chromium's
    // smooth-scroll animation itself getting interrupted/confused by this nested-scroll-container
    // layout rather than anything in the page. Disabling smooth scrolling makes every wheel tick
    // apply instantly instead of animating, trading a little visual polish for not stalling.
    private void OnBlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
    {
        e.EnvironmentOptions = new CoreWebView2EnvironmentOptions
        {
            AdditionalBrowserArguments = "--disable-smooth-scrolling"
        };
    }
}
