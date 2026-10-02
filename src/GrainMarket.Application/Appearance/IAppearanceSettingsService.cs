namespace GrainMarket.Application.Appearance;

public interface IAppearanceSettingsService
{
    Task<AppearanceSettingsDto> GetAsync(CancellationToken ct = default);
    Task<AppearanceSettingsDto> UpdateAsync(UpdateAppearanceSettingsRequest request, CancellationToken ct = default);
}
