namespace GrainMarket.Application.Trading;

public interface ITradingService
{
    Task<List<TradingProductPositionDto>> GetStockPositionAsync(DateTime? from, DateTime? to, CancellationToken ct = default);
}
