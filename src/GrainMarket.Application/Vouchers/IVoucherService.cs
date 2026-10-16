using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Vouchers;

public interface IVoucherService
{
    Task<List<VoucherDto>> GetAllAsync(VoucherType? type = null, int? seasonId = null, CancellationToken ct = default);
    Task<VoucherDto> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Read-only preview of the next PV/RV/JV number — never consumes it. The real number
    /// is only generated (and the sequence only advances) when the voucher is actually saved via
    /// CreatePaymentOrReceiptAsync, so merely opening the Payment/Receipt/Journal screen never
    /// burns one.</summary>
    Task<NextVoucherNoDto> PeekNextVoucherNoAsync(VoucherType type, CancellationToken ct = default);

    /// <summary>Creates a Payment, Receipt, or Journal (General Voucher) entry — all three post the
    /// same way (Dr the To side, Cr the From side), the only difference being VoucherType and
    /// which number sequence/prefix it draws from.</summary>
    Task<VoucherDto> CreatePaymentOrReceiptAsync(CreatePaymentOrReceiptRequest request, CancellationToken ct = default);

    /// <summary>Reverses the voucher's existing ledger postings and re-posts fresh ones for the new
    /// terms (same reverse-then-repost pattern as KachiService/PakkiService.UpdateAsync) — never
    /// mutates or deletes a previously posted LedgerEntry. Throws if the voucher is cancelled or if
    /// request implies a different voucher type than what's stored (type can't be changed by edit).</summary>
    Task<VoucherDto> UpdatePaymentOrReceiptAsync(int id, UpdatePaymentOrReceiptRequest request, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);
}
