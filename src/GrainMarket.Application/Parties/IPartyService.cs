using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Parties;

public interface IPartyService
{
    Task<List<PartyDto>> GetAllAsync(PartyType? type = null, bool includeInactive = false, CancellationToken ct = default);
    Task<PartyDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PartyDto> CreateAsync(UpsertPartyRequest request, CancellationToken ct = default);
    Task<PartyDto> UpdateAsync(int id, UpsertPartyRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
