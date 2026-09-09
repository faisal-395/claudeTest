namespace GrainMarket.Application.SaleInvoices;

public interface ISaleInvoiceService
{
    Task<List<SaleInvoiceDto>> GetAllAsync(CancellationToken ct = default);
    Task<SaleInvoiceDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SaleInvoiceDto> CreateAsync(CreateSaleInvoiceRequest request, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);
}
