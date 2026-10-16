namespace GrainMarket.Application.Purchases;

public interface IPurchaseService
{
    Task<List<PurchaseDto>> GetAllAsync(CancellationToken ct = default);
    Task<NextPurchaseInvoiceNoDto> ReserveNextInvoiceNoAsync(CancellationToken ct = default);
    Task<PurchaseDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PurchaseDto> CreateAsync(CreatePurchaseRequest request, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);

    /// <summary>Approves a Pending Purchase — posts the ledger entries that were deferred at
    /// creation time (see Role.RequiresApproval) and marks it reviewed.</summary>
    Task<PurchaseDto> ApproveAsync(int id, CancellationToken ct = default);

    /// <summary>Rejects a Pending Purchase — never posts anything. There is no edit for a Purchase
    /// today, so a rejected one must be cancelled and re-entered.</summary>
    Task<PurchaseDto> RejectAsync(int id, string? reason, CancellationToken ct = default);
}
