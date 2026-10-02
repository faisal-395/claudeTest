using GrainMarket.Domain.Entities;

namespace GrainMarket.Application.Appearance;

public interface IAppearanceSettingsService
{
    /// <summary>All scopes (Global plus every page-specific override) — the client applies them
    /// all at once so every page has its colors ready before the person navigates to it.</summary>
    Task<List<AppearanceSettingsDto>> GetAllAsync(CancellationToken ct = default);
    Task<AppearanceSettingsDto> UpdateAsync(ThemeScope scope, UpdateAppearanceSettingsRequest request, CancellationToken ct = default);
}
