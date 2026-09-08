namespace GrainMarket.Application.Trading;

/// <summary>
/// "Trading" appeared in the legacy toolbar but wasn't in the original module list. Implemented
/// here as a stock-position report for the business's own-account trading (Purchase in / Sale
/// out) — separate from the commission-based Kachi/Pakki flow, which never takes stock onto the
/// business's own books.
/// </summary>
public record TradingProductPositionDto(
    int ProductId, string ProductName, decimal PurchasedQty, decimal SoldQty, decimal NetPositionQty,
    decimal PurchaseValue, decimal SaleValue);
