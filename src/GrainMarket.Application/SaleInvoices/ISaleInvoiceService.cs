namespace GrainMarket.Application.SaleInvoices;

public interface ISaleInvoiceService
{
    Task<List<SaleInvoiceDto>> GetAllAsync(CancellationToken ct = default);
    Task<NextSaleInvoiceNoDto> ReserveNextInvoiceNoAsync(CancellationToken ct = default);
    Task<SaleInvoiceDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SaleInvoiceDto> CreateAsync(CreateSaleInvoiceRequest request, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);

    /// <summary>Approves a Pending Sale Invoice — posts the ledger entries that were deferred at
    /// creation time (see Role.RequiresApproval) and marks it reviewed.</summary>
    Task<SaleInvoiceDto> ApproveAsync(int id, CancellationToken ct = default);

    /// <summary>Rejects a Pending Sale Invoice — never posts anything. There is no edit for a Sale
    /// Invoice today, so a rejected one must be cancelled and re-entered.</summary>
    Task<SaleInvoiceDto> RejectAsync(int id, string? reason, CancellationToken ct = default);
}
