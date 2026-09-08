namespace GrainMarket.Application.Pakkis;

public interface IPakkiService
{
    Task<List<PakkiDto>> GetAllAsync(int? seasonId = null, CancellationToken ct = default);
    Task<PakkiDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PakkiDto> CreateFromKachiAsync(CreatePakkiFromKachiRequest request, CancellationToken ct = default);
    Task<PakkiDto> CreateStandaloneAsync(CreateStandalonePakkiRequest request, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);
}
