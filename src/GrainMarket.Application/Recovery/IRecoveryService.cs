namespace GrainMarket.Application.Recovery;

public interface IRecoveryService
{
    /// <summary>Parties with an outstanding balance owed TO the business (positive/debit balance).</summary>
    Task<List<OutstandingPartyDto>> GetOutstandingAsync(CancellationToken ct = default);
    Task<List<RecoveryNoteDto>> GetNotesAsync(int partyId, CancellationToken ct = default);
    Task<RecoveryNoteDto> AddNoteAsync(CreateRecoveryNoteRequest request, CancellationToken ct = default);
}
