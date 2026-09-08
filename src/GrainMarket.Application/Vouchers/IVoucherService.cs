using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Vouchers;

public interface IVoucherService
{
    Task<List<VoucherDto>> GetAllAsync(VoucherType? type = null, int? seasonId = null, CancellationToken ct = default);
    Task<VoucherDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<VoucherDto> CreatePaymentOrReceiptAsync(CreatePaymentOrReceiptRequest request, CancellationToken ct = default);
    Task<VoucherDto> CreateJournalAsync(CreateJournalRequest request, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);
}
