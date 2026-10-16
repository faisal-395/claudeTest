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

// CreditDescription/DebitDescription are only meaningful for a Journal (General Voucher) entry —
// Payment/Receipt leave them null and use the shared Description instead. They reuse the same
// Voucher.CreditDescription/DebitDescription columns the old Debit/CreditAccountId-based journal
// path wrote to (see VoucherService's comment on CancelAsync), just populated via FromType/ToType
// now: Credit pairs with From, Debit pairs with To (the same Dr-To/Cr-From convention every
// voucher type already posts with).
public record CreatePaymentOrReceiptRequest(
    VoucherType VoucherType, DateTime Date, int SeasonId, decimal Amount, string? RefNo, string? Description,
    LedgerPartyRefType FromType, int? FromPartyId, int? FromAccountId,
    LedgerPartyRefType ToType, int? ToPartyId, int? ToAccountId,
    string? CreditDescription = null, string? DebitDescription = null);

public record NextVoucherNoDto(string VoucherNo);

/// <summary>Same shape as CreatePaymentOrReceiptRequest minus VoucherType/VoucherNo — editing a
/// voucher never changes its type or number, only the terms it was raised with.</summary>
public record UpdatePaymentOrReceiptRequest(
    DateTime Date, int SeasonId, decimal Amount, string? RefNo, string? Description,
    LedgerPartyRefType FromType, int? FromPartyId, int? FromAccountId,
    LedgerPartyRefType ToType, int? ToPartyId, int? ToAccountId,
    string? CreditDescription = null, string? DebitDescription = null);
