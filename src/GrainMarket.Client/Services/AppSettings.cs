namespace GrainMarket.Client.Services;

/// <summary>
/// The API base URL is stored in device Preferences, not hardcoded — so moving from
/// "the API runs on this same till PC" to "the API runs on the LAN server" (see README
/// "Architecture & deployment") is a Settings screen change, never a rebuild.
/// </summary>
public class AppSettings
{
    private const string ApiBaseUrlKey = "ApiBaseUrl";

    // Matches GrainMarket.Api/Properties/launchSettings.json's "https" profile (the one Visual
    // Studio uses by default). If you run the API on the "http" profile instead, or change the
    // ports in launchSettings.json, update this via the app's own Connection Settings screen —
    // no rebuild needed.
    private const string DefaultApiBaseUrl = "https://localhost:7200/";

    public string ApiBaseUrl
    {
        get => Preferences.Default.Get(ApiBaseUrlKey, DefaultApiBaseUrl);
        set => Preferences.Default.Set(ApiBaseUrlKey, value);
    }
}
