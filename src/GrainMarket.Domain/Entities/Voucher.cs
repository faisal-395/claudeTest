using GrainMarket.Domain.Common;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Domain.Entities;

/// <summary>
/// Shared entity for Payment, Receipt and Journal (General Voucher) vouchers. All three use
/// From*/To* (Cash, Bank, Party, or Account) — Credit pairs with From, Debit with To, the same
/// Dr-To/Cr-From convention every voucher posts with. DebitAccountId/CreditAccountId are legacy:
/// a Journal voucher raised before it gained From*/To* picking still has those populated instead
/// (FromType null identifies it), kept only so that old data stays readable. DebitDescription/
/// CreditDescription are populated by Journal (via From*/To*) for its own per-side description —
/// Payment/Receipt leave them null and use the shared Description instead.
/// Every voucher posts one Dr + one Cr row to LedgerEntry.
/// </summary>
public class Voucher : BaseEntity
{
    public VoucherType VoucherType { get; set; }
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;

    public decimal Amount { get; set; }
    public string? RefNo { get; set; }
    public string? Description { get; set; }

    // Payment / Receipt
    public LedgerPartyRefType? FromType { get; set; }
    public int? FromPartyId { get; set; }
    public Party? FromParty { get; set; }
    public int? FromAccountId { get; set; }
    public ChartOfAccount? FromAccount { get; set; }

    public LedgerPartyRefType? ToType { get; set; }
    public int? ToPartyId { get; set; }
    public Party? ToParty { get; set; }
    public int? ToAccountId { get; set; }
    public ChartOfAccount? ToAccount { get; set; }

    // Journal (legacy — see class summary)
    public int? DebitAccountId { get; set; }
    public ChartOfAccount? DebitAccount { get; set; }
    public string? DebitDescription { get; set; }

    public int? CreditAccountId { get; set; }
    public ChartOfAccount? CreditAccount { get; set; }
    public string? CreditDescription { get; set; }

    public bool IsCancelled { get; set; }
}
