namespace GrainMarket.Application.Stock;

public record StockDto(
    int ProductId, string ProductName, string? NameUrdu, string BaseUnit,
    decimal PurchasedQty, decimal SoldQty, decimal OnHandQty);

/// <summary>Sale Invoice's price suggestion for a product, priced on LIFO cost — always the single
/// most recent purchase price for this product, so a new purchase immediately becomes the pricing
/// basis regardless of how much older (cheaper or costlier) stock is still on hand. Used to auto-fill
/// the Price field when a line's product is chosen.</summary>
public record SuggestedSalePriceDto(decimal AvailableQty, decimal LatestCost, decimal SuggestedPrice);

/// <summary>One purchase lot within 15 days of (or past) its expiry date and still holding stock.</summary>
public record ExpiringLotDto(
    int ProductId, string ProductName, string? NameUrdu, string BaseUnit,
    int PurchaseLineId, DateTime ExpiryDate, decimal RemainingQuantity, int DaysUntilExpiry);
