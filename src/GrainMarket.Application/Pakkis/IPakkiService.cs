namespace GrainMarket.Application.Pakkis;

public interface IPakkiService
{
    Task<List<PakkiDto>> GetAllAsync(int? seasonId = null, CancellationToken ct = default);
    Task<PakkiDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PakkiDto> CreateFromKachiAsync(CreatePakkiFromKachiRequest request, CancellationToken ct = default);
    Task<PakkiDto> CreateStandaloneAsync(CreateStandalonePakkiRequest request, CancellationToken ct = default);
    Task<PakkiDto> UpdateAsync(int id, UpdatePakkiRequest request, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);

    /// <summary>Approves a Pending Pakki — posts the ledger entries that were deferred at creation
    /// time (see Role.RequiresApproval) and marks it reviewed.</summary>
    Task<PakkiDto> ApproveAsync(int id, CancellationToken ct = default);

    /// <summary>Rejects a Pending Pakki — never posts anything. The original submitter or anyone
    /// with Approvals edit permission can then correct it via UpdateAsync, which resubmits it.</summary>
    Task<PakkiDto> RejectAsync(int id, string? reason, CancellationToken ct = default);
}
