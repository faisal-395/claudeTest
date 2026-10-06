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

            var created = NewDefaultsFor(scope);
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
            settings = NewDefaultsFor(scope);
            _db.AppearanceSettings.Add(settings);
        }

        settings.PrimaryColor = request.PrimaryColor;
        settings.AccentColor = request.AccentColor;
        settings.SurfaceColor = request.SurfaceColor;
        settings.PanelColor = request.PanelColor;
        settings.ProductPanelColor = request.ProductPanelColor;
        settings.CreditPanelColor = request.CreditPanelColor;
        settings.DebitPanelColor = request.DebitPanelColor;
        settings.HeadingBackgroundColor = request.HeadingBackgroundColor;
        settings.InputBackgroundColor = request.InputBackgroundColor;
        settings.GridHeaderColor = request.GridHeaderColor;
        settings.GridBackgroundColor = request.GridBackgroundColor;
        settings.LabelFontSizePx = request.LabelFontSizePx;
        settings.GridFullBorders = request.GridFullBorders;
        settings.NavigationLayout = request.NavigationLayout;
        settings.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(settings);
    }

    // Every scope otherwise shares the same class-level defaults (AppearanceSettings' own property
    // initializers) — Kachi, Pakki, and SaleInvoice are the exceptions, matching the legacy
    // software's own look (green/blue/purple respectively) for a brand-new row, while staying
    // fully editable afterwards from Setup > Appearance like any other scope's colors.
    // HeadingBackgroundColor is kept light enough on every one of them to pass the
    // PrimaryColor/HeadingBackgroundColor contrast check against the default (dark) PrimaryColor —
    // see UpdateAppearanceSettingsRequestValidator; the other fields (ProductPanelColor,
    // GridHeaderColor, GridBackgroundColor) each get their own auto-computed contrast text color
    // (see index.html's applyTheme) so they can stay closer to the legacy screenshot's actual
    // saturation.
    private static AppearanceSettings NewDefaultsFor(ThemeScope scope)
    {
        var settings = new AppearanceSettings { Scope = scope };
        switch (scope)
        {
            case ThemeScope.Kachi:
                settings.HeadingBackgroundColor = "#C8E6C9";
                settings.PanelColor = "#E8F5E9";
                settings.GridHeaderColor = "#A5D6A7";
                settings.GridBackgroundColor = "#F1F8F2";
                break;
            case ThemeScope.Pakki:
                settings.HeadingBackgroundColor = "#BBDEFB";
                settings.PanelColor = "#E3F2FD";
                settings.GridHeaderColor = "#90CAF9";
                settings.GridBackgroundColor = "#F2F8FD";
                break;
            case ThemeScope.SaleInvoice:
                settings.HeadingBackgroundColor = "#C5AEE0";
                settings.ProductPanelColor = "#D9C7EC";
                settings.GridHeaderColor = "#B7C2D1";
                settings.GridBackgroundColor = "#D3DAE3";
                break;
        }
        return settings;
    }

    private static AppearanceSettingsDto ToDto(AppearanceSettings s) => new(
        s.Id, s.Scope, s.PrimaryColor, s.AccentColor, s.SurfaceColor,
        s.PanelColor, s.ProductPanelColor, s.CreditPanelColor, s.DebitPanelColor,
        s.HeadingBackgroundColor, s.InputBackgroundColor,
        s.GridHeaderColor, s.GridBackgroundColor,
        s.LabelFontSizePx, s.GridFullBorders, s.NavigationLayout);
}
