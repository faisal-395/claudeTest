using GrainMarket.Domain.Entities;

namespace GrainMarket.Application.Appearance;

public record AppearanceSettingsDto(
    int Id, ThemeScope Scope, string PrimaryColor, string AccentColor, string SurfaceColor,
    string PanelColor, string InputBackgroundColor, string InputBorderColor, string GridHeaderColor,
    int LabelFontSizePx);

public record UpdateAppearanceSettingsRequest(
    string PrimaryColor, string AccentColor, string SurfaceColor,
    string PanelColor, string InputBackgroundColor, string InputBorderColor, string GridHeaderColor,
    int LabelFontSizePx);
