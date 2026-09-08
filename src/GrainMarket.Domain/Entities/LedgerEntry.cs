using GrainMarket.Domain.Common;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Domain.Entities;

/// <summary>
/// The single source of truth for every account/party balance. Every transaction (Kachi, Pakki,
/// Sale, Purchase, Payment, Receipt, Journal, Expense) posts one or more rows here as part of the
/// same transaction that creates it. Reports and the Ledger screen only ever read this table —
/// balances are never re-derived by summing the transaction tables live.
/// Exactly one of PartyId / ChartOfAccountId is set per row (a party sub-ledger row, or a GL
/// chart-of-accounts row).
/// </summary>
public class LedgerEntry : BaseEntity
{
    public DateTime Date { get; set; }

    public int? PartyId { get; set; }
    public Party? Party { get; set; }

    public int? ChartOfAccountId { get; set; }
    public ChartOfAccount? ChartOfAccount { get; set; }

    public decimal Debit { get; set; }
    public decimal Credit { get; set; }

    /// <summary>Running balance for this ledger (party or account), computed at posting time.</summary>
    public decimal RunningBalance { get; set; }

    public LedgerSourceType SourceType { get; set; }
    public int SourceId { get; set; }

    public string? Description { get; set; }
}
