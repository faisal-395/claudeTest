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

/// <summary>Opening (balance strictly before From) and Closing (balance on or before To) for
/// every account/party in one bulk call — the per-entity equivalent of what
/// GetAccountLedgerAsync/GetPartyLedgerAsync already compute for a single one, used by the Account
/// Balance Report's date range instead of always reading "right now" (ChartOfAccountDto/PartyDto's
/// own CurrentBalance). Leaving From/To both null degenerates to Opening=0, Closing=the same
/// right-now balance those DTOs already carry.</summary>
public record AccountBalanceDto(int AccountId, string Code, string Name, string? NameUrdu, string AccountTypeName, decimal OpeningBalance, decimal ClosingBalance);
public record PartyBalanceDto(int PartyId, string Name, string? NameUrdu, PartyType PartyType, decimal OpeningBalance, decimal ClosingBalance);
public record PeriodBalancesDto(List<AccountBalanceDto> Accounts, List<PartyBalanceDto> Parties);
