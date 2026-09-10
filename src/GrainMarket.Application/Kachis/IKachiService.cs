namespace GrainMarket.Application.Kachis;

public interface IKachiService
{
    Task<List<KachiDto>> GetAllAsync(int? seasonId = null, CancellationToken ct = default);
    Task<KachiDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<KachiDto> CreateAsync(CreateKachiRequest request, CancellationToken ct = default);
    Task<KachiDto> UpdateAsync(int id, UpdateKachiRequest request, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);

    /// <summary>Reverses this Kachi's own ledger postings without cancelling it — called right
    /// before converting it to a Pakki, since the Pakki then posts its own authoritative
    /// farmer/buyer entries for the same underlying transaction.</summary>
    Task ReverseLedgerForConversionAsync(int kachiId, CancellationToken ct = default);

    /// <summary>Re-posts this Kachi's ledger entries from its own stored fields — the mirror image
    /// of ReverseLedgerForConversionAsync, called when the Pakki it was converted to gets
    /// cancelled and the Kachi reverts to Open, so it isn't left with no ledger presence at all.</summary>
    Task RepostLedgerAfterPakkiCancellationAsync(int kachiId, CancellationToken ct = default);
}
