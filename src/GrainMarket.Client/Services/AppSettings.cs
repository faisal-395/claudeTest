namespace GrainMarket.Client.Services;

/// <summary>
/// The API base URL is stored in device Preferences, not hardcoded — so moving from
/// "the API runs on this same till PC" to "the API runs on the LAN server" (see README
/// "Architecture & deployment") is a Settings screen change, never a rebuild.
/// </summary>
public class AppSettings
{
    private const string ApiBaseUrlKey = "ApiBaseUrl";
    private const string DefaultApiBaseUrl = "http://localhost:5080/";

    public string ApiBaseUrl
    {
        get => Preferences.Default.Get(ApiBaseUrlKey, DefaultApiBaseUrl);
        set => Preferences.Default.Set(ApiBaseUrlKey, value);
    }
}
