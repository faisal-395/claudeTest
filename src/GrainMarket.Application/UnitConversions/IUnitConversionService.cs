namespace GrainMarket.Application.UnitConversions;

public interface IUnitConversionService
{
    Task<List<UnitConversionDto>> GetAllAsync(CancellationToken ct = default);
    Task<UnitConversionDto> CreateAsync(UpsertUnitConversionRequest request, CancellationToken ct = default);
    Task<UnitConversionDto> UpdateAsync(int id, UpsertUnitConversionRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
