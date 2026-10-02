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

    public async Task<AppearanceSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var settings = await GetOrCreateAsync(ct);
        return ToDto(settings);
    }

    public async Task<AppearanceSettingsDto> UpdateAsync(UpdateAppearanceSettingsRequest request, CancellationToken ct = default)
    {
        var settings = await GetOrCreateAsync(ct);

        settings.PrimaryColor = request.PrimaryColor;
        settings.AccentColor = request.AccentColor;
        settings.SurfaceColor = request.SurfaceColor;
        settings.PanelColor = request.PanelColor;
        settings.InputBackgroundColor = request.InputBackgroundColor;
        settings.InputBorderColor = request.InputBorderColor;
        settings.GridHeaderColor = request.GridHeaderColor;
        settings.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(settings);
    }

    // A migration always seeds one row, but self-heal instead of throwing if it's ever missing —
    // every authenticated page depends on this never failing, since the theme is fetched on login.
    private async Task<AppearanceSettings> GetOrCreateAsync(CancellationToken ct)
    {
        var settings = await _db.AppearanceSettings.FirstOrDefaultAsync(ct);
        if (settings is not null) return settings;

        settings = new AppearanceSettings();
        _db.AppearanceSettings.Add(settings);
        await _db.SaveChangesAsync(ct);
        return settings;
    }

    private static AppearanceSettingsDto ToDto(AppearanceSettings s) => new(
        s.Id, s.PrimaryColor, s.AccentColor, s.SurfaceColor,
        s.PanelColor, s.InputBackgroundColor, s.InputBorderColor, s.GridHeaderColor);
}
