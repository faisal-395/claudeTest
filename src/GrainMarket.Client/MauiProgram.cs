using GrainMarket.Client.Services;
using Microsoft.Extensions.Logging;

namespace GrainMarket.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Windows throttles a WebView2 renderer that hasn't had user input in a while (part of
        // Chromium's background-tab power-saving, which WebView2 applies even to a window that's
        // merely idle, not actually backgrounded) — timers and pending JS get suspended, and
        // resuming that renderer on the next click can misfire and show the framework's default
        // "An unhandled error has occurred" overlay, on any screen, with no application code
        // involved (this reproduces on the login page, which never runs a timer or a background
        // task of its own). Must be set before WebView2 creates its environment, so first line.
        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS",
            "--disable-backgrounding-occluded-windows --disable-renderer-backgrounding --disable-background-timer-throttling --disable-features=CalculateNativeWinOcclusion");

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // Bundled so Urdu labels and print templates never depend on the till PC having
                // the font installed — see Resources/Fonts and wwwroot/css/app.css @font-face.
                fonts.AddFont("JameelNooriNastaleeq.ttf", "JameelNooriNastaleeq");
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<AppSettings>();
        builder.Services.AddSingleton<AuthState>();
        builder.Services.AddSingleton<TokenAuthHandler>();

        // Base URL points at localhost today (API runs as a Windows service on the same till PC)
        // and gets repointed at the LAN server's address later purely via appsettings — see
        // README "Architecture & deployment". No client code changes when that move happens.
        builder.Services.AddHttpClient<ApiClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            client.BaseAddress = new Uri(settings.ApiBaseUrl);
        }).AddHttpMessageHandler<TokenAuthHandler>();

        // Deliberately has no TokenAuthHandler — used only by TokenAuthHandler itself to call
        // api/auth/refresh, which must go out unauthenticated (it carries its own refresh token).
        // Attaching the same handler here would recurse into itself.
        builder.Services.AddHttpClient("AuthRefresh", (sp, client) =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            client.BaseAddress = new Uri(settings.ApiBaseUrl);
        });

        return builder.Build();
    }
}
