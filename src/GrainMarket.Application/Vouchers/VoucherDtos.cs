using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Vouchers;

public record VoucherDto(
    int Id, VoucherType VoucherType, string VoucherNo, DateTime Date, int SeasonId, string SeasonName,
    decimal Amount, string? RefNo, string? Description,
    LedgerPartyRefType? FromType, int? FromPartyId, string? FromPartyName, int? FromAccountId, string? FromAccountName, decimal? FromBalance,
    LedgerPartyRefType? ToType, int? ToPartyId, string? ToPartyName, int? ToAccountId, string? ToAccountName, decimal? ToBalance,
    int? DebitAccountId, string? DebitAccountName, string? DebitDescription,
    int? CreditAccountId, string? CreditAccountName, string? CreditDescription,
    bool IsCancelled);

public record CreatePaymentOrReceiptRequest(
    VoucherType VoucherType, DateTime Date, int SeasonId, decimal Amount, string? RefNo, string? Description,
    LedgerPartyRefType FromType, int? FromPartyId, int? FromAccountId,
    LedgerPartyRefType ToType, int? ToPartyId, int? ToAccountId);

public record CreateJournalRequest(
    DateTime Date, int SeasonId, decimal Amount, string? RefNo,
    int DebitAccountId, string? DebitDescription,
    int CreditAccountId, string? CreditDescription);
