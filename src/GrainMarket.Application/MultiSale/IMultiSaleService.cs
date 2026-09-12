namespace GrainMarket.Application.MultiSale;

public interface IMultiSaleService
{
    Task<MultiSaleResultDto> CreateAsync(CreateMultiSaleRequest request, CancellationToken ct = default);
}
