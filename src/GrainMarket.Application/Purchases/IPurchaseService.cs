namespace GrainMarket.Application.Purchases;

public interface IPurchaseService
{
    Task<List<PurchaseDto>> GetAllAsync(CancellationToken ct = default);
    Task<PurchaseDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PurchaseDto> CreateAsync(CreatePurchaseRequest request, CancellationToken ct = default);
}
