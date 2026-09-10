using GrainMarket.Client.Services;
using Microsoft.Extensions.Logging;

namespace GrainMarket.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Mitigates a separate, minor WebView2 quirk (Chromium sometimes misjudges a fully
        // visible, focused window as occluded and throttles its timers) — harmless to leave on.
        // Must be set before WebView2 creates its environment, so first line.
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

        // Must be Transient, not Singleton: IHttpClientFactory rebuilds ApiClient's handler
        // pipeline every 2 minutes by default (HandlerLifetime, to pick up DNS changes), and each
        // rebuild resolves TokenAuthHandler from DI again. A singleton hands back the same
        // instance every time — but that instance's InnerHandler is already set from the
        // previous build, and HttpMessageHandlerBuilder refuses to reuse a handler whose
        // InnerHandler isn't null, throwing "The 'InnerHandler' property must be null." on the
        // very next request after ~2 minutes idle, on ANY page (this is what was actually behind
        // the "unhandled error after being idle" reports, including on the login screen before
        // any auth logic ever ran). Transient gives the factory a fresh instance on every
        // rebuild, which is fine — TokenAuthHandler holds no state that needs to outlive one
        // pipeline (AuthState, the thing it actually cares about, is already its own singleton).
        builder.Services.AddTransient<TokenAuthHandler>();

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
