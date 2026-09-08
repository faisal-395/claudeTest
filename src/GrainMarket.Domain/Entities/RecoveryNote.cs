using GrainMarket.Domain.Common;

namespace GrainMarket.Domain.Entities;

/// <summary>Follow-up log for outstanding recovery (contact attempt, promised payment date, etc.).
/// Recovery itself is a report/view over LedgerEntry + Party, not a ledger table.</summary>
public class RecoveryNote : BaseEntity
{
    public int PartyId { get; set; }
    public Party Party { get; set; } = null!;

    public DateTime ContactDate { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime? PromisedDate { get; set; }
    public decimal? PromisedAmount { get; set; }
}
