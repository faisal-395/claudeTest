namespace GrainMarket.Application.Stock;

public record StockDto(
    int ProductId, string ProductName, string? NameUrdu, string BaseUnit,
    decimal PurchasedQty, decimal SoldQty, decimal OnHandQty);
