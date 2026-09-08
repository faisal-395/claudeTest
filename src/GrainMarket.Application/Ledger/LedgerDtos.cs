using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Ledger;

public record LedgerRowDto(int Id, DateTime Date, decimal Debit, decimal Credit, decimal RunningBalance, LedgerSourceType SourceType, int SourceId, string? Description);

public record PartyLedgerDto(int PartyId, string PartyName, decimal OpeningBalance, decimal ClosingBalance, List<LedgerRowDto> Rows);

public record AccountLedgerDto(int AccountId, string AccountCode, string AccountName, decimal ClosingBalance, List<LedgerRowDto> Rows);
