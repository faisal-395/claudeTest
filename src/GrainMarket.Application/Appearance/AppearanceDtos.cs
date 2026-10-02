namespace GrainMarket.Application.Appearance;

public record AppearanceSettingsDto(
    int Id, string PrimaryColor, string AccentColor, string SurfaceColor,
    string PanelColor, string InputBackgroundColor, string InputBorderColor, string GridHeaderColor);

public record UpdateAppearanceSettingsRequest(
    string PrimaryColor, string AccentColor, string SurfaceColor,
    string PanelColor, string InputBackgroundColor, string InputBorderColor, string GridHeaderColor);
