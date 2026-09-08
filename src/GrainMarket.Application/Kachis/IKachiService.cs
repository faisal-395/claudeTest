namespace GrainMarket.Application.Kachis;

public interface IKachiService
{
    Task<List<KachiDto>> GetAllAsync(int? seasonId = null, CancellationToken ct = default);
    Task<KachiDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<KachiDto> CreateAsync(CreateKachiRequest request, CancellationToken ct = default);
    Task<KachiDto> UpdateAsync(int id, UpdateKachiRequest request, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);
}
