using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Common.Interfaces;

public interface ILedgerPostingService
{
    /// <summary>Posts a single debit or credit row against a party's sub-ledger and returns the new running balance.</summary>
    Task<decimal> PostPartyEntryAsync(int partyId, DateTime date, decimal debit, decimal credit, LedgerSourceType sourceType, int sourceId, string? description, CancellationToken ct = default);

    /// <summary>Posts a single debit or credit row against a chart-of-accounts ledger and returns the new running balance.</summary>
    Task<decimal> PostAccountEntryAsync(int chartOfAccountId, DateTime date, decimal debit, decimal credit, LedgerSourceType sourceType, int sourceId, string? description, CancellationToken ct = default);
}
