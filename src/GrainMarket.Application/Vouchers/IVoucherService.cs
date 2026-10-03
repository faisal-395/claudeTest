using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Vouchers;

public interface IVoucherService
{
    Task<List<VoucherDto>> GetAllAsync(VoucherType? type = null, int? seasonId = null, CancellationToken ct = default);
    Task<VoucherDto> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Reserves (consumes) the next PV/RV number immediately, so the Payment/Receipt
    /// screen can show it before Save — matches Purchase/Sale Invoice's reserve-ahead pattern. Left
    /// unsaved, that number is simply skipped. VoucherType.Journal is not a valid argument here.</summary>
    Task<NextVoucherNoDto> ReserveNextVoucherNoAsync(VoucherType type, CancellationToken ct = default);
    Task<VoucherDto> CreatePaymentOrReceiptAsync(CreatePaymentOrReceiptRequest request, CancellationToken ct = default);
    Task<VoucherDto> CreateJournalAsync(CreateJournalRequest request, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);
}
