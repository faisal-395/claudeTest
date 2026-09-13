using GrainMarket.Domain.Entities;

namespace GrainMarket.Application.Stock;

public interface IStockService
{
    Task<List<StockDto>> GetStockAsync(CancellationToken ct = default);
    Task<SuggestedSalePriceDto> GetSuggestedSalePriceAsync(int productId, decimal quantity, CancellationToken ct = default);
    Task<List<ExpiringLotDto>> GetExpiringLotsAsync(int withinDays, CancellationToken ct = default);

    /// <summary>Consumes <paramref name="quantity"/> of <paramref name="productId"/> from the oldest
    /// available purchase lots first, decrementing each lot's RemainingQuantity on the tracked entity
    /// (the caller's own SaveChangesAsync persists it) and returning the per-lot cost breakdown to
    /// store as SaleInvoiceLineAllocation rows. Throws InvalidCalculationException if on-hand stock
    /// is insufficient — nothing is mutated in that case.</summary>
    Task<List<FifoAllocation>> AllocateFifoAsync(int productId, decimal quantity, CancellationToken ct = default);
}

public record FifoAllocation(PurchaseLine PurchaseLine, decimal Quantity, decimal UnitCost);
