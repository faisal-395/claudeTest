namespace GrainMarket.Application.Stock;

public interface IStockService
{
    Task<List<StockDto>> GetStockAsync(CancellationToken ct = default);
    Task<decimal> GetOnHandQtyAsync(int productId, CancellationToken ct = default);
}
