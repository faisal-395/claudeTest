using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Appearance;

public class AppearanceSettingsService : IAppearanceSettingsService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AppearanceSettingsService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<List<AppearanceSettingsDto>> GetAllAsync(CancellationToken ct = default)
    {
        var existing = await _db.AppearanceSettings.ToListAsync(ct);

        // A migration always seeds one row per ThemeScope, but self-heal instead of throwing if any
        // is ever missing — every authenticated page depends on this never failing, since the theme
        // is fetched on login.
        foreach (var scope in Enum.GetValues<ThemeScope>())
        {
            if (existing.Any(s => s.Scope == scope)) continue;

            var created = new AppearanceSettings { Scope = scope };
            _db.AppearanceSettings.Add(created);
            existing.Add(created);
        }

        await _db.SaveChangesAsync(ct);
        return existing.OrderBy(s => s.Scope).Select(ToDto).ToList();
    }

    public async Task<AppearanceSettingsDto> UpdateAsync(ThemeScope scope, UpdateAppearanceSettingsRequest request, CancellationToken ct = default)
    {
        var settings = await _db.AppearanceSettings.FirstOrDefaultAsync(s => s.Scope == scope, ct);
        if (settings is null)
        {
            settings = new AppearanceSettings { Scope = scope };
            _db.AppearanceSettings.Add(settings);
        }

        settings.PrimaryColor = request.PrimaryColor;
        settings.AccentColor = request.AccentColor;
        settings.SurfaceColor = request.SurfaceColor;
        settings.PanelColor = request.PanelColor;
        settings.HeadingBackgroundColor = request.HeadingBackgroundColor;
        settings.InputBackgroundColor = request.InputBackgroundColor;
        settings.GridHeaderColor = request.GridHeaderColor;
        settings.LabelFontSizePx = request.LabelFontSizePx;
        settings.GridFullBorders = request.GridFullBorders;
        settings.NavigationLayout = request.NavigationLayout;
        settings.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(settings);
    }

    private static AppearanceSettingsDto ToDto(AppearanceSettings s) => new(
        s.Id, s.Scope, s.PrimaryColor, s.AccentColor, s.SurfaceColor,
        s.PanelColor, s.HeadingBackgroundColor, s.InputBackgroundColor, s.GridHeaderColor, s.LabelFontSizePx,
        s.GridFullBorders, s.NavigationLayout);
}
