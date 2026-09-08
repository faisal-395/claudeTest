namespace GrainMarket.Application.Seasons;

public interface ISeasonService
{
    Task<List<SeasonDto>> GetAllAsync(CancellationToken ct = default);
    Task<SeasonDto> CreateAsync(UpsertSeasonRequest request, CancellationToken ct = default);
    Task<SeasonDto> UpdateAsync(int id, UpsertSeasonRequest request, CancellationToken ct = default);
}
