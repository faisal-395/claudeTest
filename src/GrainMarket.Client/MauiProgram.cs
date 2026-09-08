using GrainMarket.Client.Services;
using Microsoft.Extensions.Logging;

namespace GrainMarket.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
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

        return builder.Build();
    }
}
