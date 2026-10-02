namespace GrainMarket.Application.Appearance;

public record AppearanceSettingsDto(int Id, string PrimaryColor, string AccentColor, string SurfaceColor);

public record UpdateAppearanceSettingsRequest(string PrimaryColor, string AccentColor, string SurfaceColor);
