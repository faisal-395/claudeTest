namespace GrainMarket.Application.MultiPurchase;

public interface IMultiPurchaseService
{
    Task<MultiPurchaseResultDto> CreateAsync(CreateMultiPurchaseRequest request, CancellationToken ct = default);
}
