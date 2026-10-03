using GrainMarket.Domain.Entities;

namespace GrainMarket.Application.Appearance;

public record AppearanceSettingsDto(
    int Id, ThemeScope Scope, string PrimaryColor, string AccentColor, string SurfaceColor,
    string PanelColor, string ProductPanelColor, string HeadingBackgroundColor, string InputBackgroundColor,
    string GridHeaderColor, string GridBackgroundColor,
    int LabelFontSizePx, bool GridFullBorders, NavigationLayout NavigationLayout);

public record UpdateAppearanceSettingsRequest(
    string PrimaryColor, string AccentColor, string SurfaceColor,
    string PanelColor, string ProductPanelColor, string HeadingBackgroundColor, string InputBackgroundColor,
    string GridHeaderColor, string GridBackgroundColor,
    int LabelFontSizePx, bool GridFullBorders, NavigationLayout NavigationLayout);
