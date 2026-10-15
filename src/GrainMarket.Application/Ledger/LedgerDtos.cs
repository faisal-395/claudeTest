using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Ledger;

public record LedgerRowDto(int Id, DateTime Date, DateTime CreatedAtUtc, decimal Debit, decimal Credit, decimal RunningBalance, LedgerSourceType SourceType, int SourceId, string? Description, string? ReferenceNo);

public record PartyLedgerDto(int PartyId, string PartyName, decimal OpeningBalance, decimal ClosingBalance, List<LedgerRowDto> Rows);

public record AccountLedgerDto(int AccountId, string AccountCode, string AccountName, decimal OpeningBalance, decimal ClosingBalance, List<LedgerRowDto> Rows);

/// <summary>One row of the Day Book — every ledger entry (party or account, any source) posted on
/// a single date, across the whole business, not scoped to one party/account the way
/// PartyLedgerDto/AccountLedgerDto are.</summary>
public record DayBookRowDto(
    int Id, DateTime Date, string EntityName, bool IsParty,
    decimal Debit, decimal Credit, LedgerSourceType SourceType, int SourceId,
    string? Description, string? ReferenceNo);
