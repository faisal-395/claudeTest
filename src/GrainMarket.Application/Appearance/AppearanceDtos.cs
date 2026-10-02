using GrainMarket.Domain.Entities;

namespace GrainMarket.Application.Appearance;

public record AppearanceSettingsDto(
    int Id, ThemeScope Scope, string PrimaryColor, string AccentColor, string SurfaceColor,
    string PanelColor, string HeadingBackgroundColor, string InputBackgroundColor, string GridHeaderColor,
    int LabelFontSizePx);

public record UpdateAppearanceSettingsRequest(
    string PrimaryColor, string AccentColor, string SurfaceColor,
    string PanelColor, string HeadingBackgroundColor, string InputBackgroundColor, string GridHeaderColor,
    int LabelFontSizePx);
