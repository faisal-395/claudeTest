namespace GrainMarket.Application.Stock;

public record StockDto(
    int ProductId, string ProductName, string? NameUrdu, string BaseUnit,
    decimal PurchasedQty, decimal SoldQty, decimal OnHandQty);

/// <summary>Preview of what a Sale Invoice line would cost/charge for a given quantity, computed from
/// the FIFO-weighted average cost of the lots that would actually be consumed — without allocating
/// anything. Used to auto-fill (and re-fill on quantity change) the Price field.</summary>
public record SuggestedSalePriceDto(decimal AvailableQty, decimal AverageCost, decimal SuggestedPrice);

/// <summary>One purchase lot within 15 days of (or past) its expiry date and still holding stock.</summary>
public record ExpiringLotDto(
    int ProductId, string ProductName, string? NameUrdu, string BaseUnit,
    int PurchaseLineId, DateTime ExpiryDate, decimal RemainingQuantity, int DaysUntilExpiry);
